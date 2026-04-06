using BigDaddyProject.Application.DTOs;
using BigDaddyProject.Application.Interfaces;
using BigDaddyProject.Domain.Common;
using BigDaddyProject.Domain.Entities;
using BigDaddyProject.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BigDaddyProject.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _um;
    private readonly ITokenService _token;
    private readonly IPermissionService _perms;
    private readonly IAuditService _audit;
    private readonly IEmailService _email;
    private readonly AppDbContext _db;

    public AuthService(UserManager<ApplicationUser> um, ITokenService token,
        IPermissionService perms, IAuditService audit, IEmailService email, AppDbContext db)
    {
        _um = um; _token = token; _perms = perms;
        _audit = audit; _email = email; _db = db;
    }

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest req)
    {
        var user = await _um.FindByEmailAsync(req.Email);
        if (user == null)
            return Result<LoginResponse>.Failure("Invalid email or password.", 401);

        if (user.Status != "Active")
        {
            await _audit.LogAsync(user.Id, "LoginFailed", "Inactive account");
            return Result<LoginResponse>.Failure("Account is inactive.", 401);
        }

        if (await _um.IsLockedOutAsync(user))
        {
            await _audit.LogAsync(user.Id, "LoginFailed", "Account locked");
            return Result<LoginResponse>.Failure("Account is locked. Try again later.", 401);
        }

        if (!await _um.CheckPasswordAsync(user, req.Password))
        {
            await _um.AccessFailedAsync(user);
            await _audit.LogAsync(user.Id, "LoginFailed", "Wrong password");
            return Result<LoginResponse>.Failure("Invalid email or password.", 401);
        }

        // Device registration check (skip for Web)
        if (!string.IsNullOrEmpty(req.DeviceId) && req.DeviceType != "Web")
        {
            var existing = await _db.UserDevices
                .FirstOrDefaultAsync(d => d.UserId == user.Id
                    && d.DeviceType == req.DeviceType && d.IsActive);

            if (existing != null && existing.DeviceId != req.DeviceId)
            {
                if (!req.DeviceConfirmed)
                {
                    // Return 200 with flag — UI shows confirmation dialog
                    return Result<LoginResponse>.Success(new LoginResponse(
                        Token: "", RefreshToken: "", ExpiresAt: DateTime.UtcNow,
                        UserInfo: null!,
                        RequiresDeviceConfirmation: true,
                        DeviceMessage: $"Logging in on this {req.DeviceType} will sign out your previously registered {req.DeviceType}. Continue?"));
                }
                existing.IsActive = false;  // deactivate old device
            }

            var device = await _db.UserDevices
                .FirstOrDefaultAsync(d => d.UserId == user.Id && d.DeviceId == req.DeviceId);
            if (device == null)
            {
                _db.UserDevices.Add(new UserDevice
                {
                    UserId = user.Id,
                    DeviceId = req.DeviceId,
                    DeviceType = req.DeviceType!,
                    DeviceName = req.DeviceName,
                    Platform = req.Platform,
                    AppVersion = req.AppVersion,
                    IsActive = true,
                    LastLogin = DateTime.UtcNow
                });
            }
            else { device.IsActive = true; device.LastLogin = DateTime.UtcNow; }
            await _db.SaveChangesAsync();
        }

        await _um.ResetAccessFailedCountAsync(user);

        var roles = await _um.GetRolesAsync(user);
        var permissions = await _perms.GetEffectivePermissionsAsync(user.Id);
        var claims = BuildClaims(user, roles);
        var accessToken = _token.GenerateAccessToken(claims);
        var refreshToken = _token.GenerateRefreshToken();
        var expiry = _token.GetTokenExpiry();

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        });
        await _db.SaveChangesAsync();

        await _audit.LogAsync(user.Id, "LoginSuccess", $"Device: {req.DeviceType ?? "Web"}");

        var session = new UserSession(
            user.Id, user.Name ?? user.Email!, user.Email!,
            user.Mobile, user.Photo, user.Status,
            user.ExpirationDate, roles.ToList(), permissions);

        return Result<LoginResponse>.Success(
            new LoginResponse(accessToken, refreshToken, expiry, session));
    }

    public async Task<Result> LogoutAsync(int userId)
    {
        await _db.RefreshTokens
            .Where(r => r.UserId == userId && !r.IsRevoked)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.IsRevoked, true));
        await _audit.LogAsync(userId, "Logout");
        return Result.Success();
    }

    public async Task<Result> ChangePasswordAsync(int userId, ChangePasswordRequest dto)
    {
        if (dto.NewPassword != dto.ConfirmNewPassword)
            return Result.Failure("Passwords do not match.");

        var user = await _um.FindByIdAsync(userId.ToString());
        if (user == null) return Result.Failure("User not found.", 404);

        var result = await _um.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded)
            return Result.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));

        user.MustChangePassword = false;
        await _um.UpdateAsync(user);
        await _audit.LogAsync(userId, "PasswordChanged");
        return Result.Success();
    }

    public async Task<Result> ForgotPasswordAsync(ForgotPasswordRequest dto)
    {
        var user = await _um.FindByEmailAsync(dto.Email);
        if (user == null || user.Status != "Active") return Result.Success(); // silent — no email enumeration

        var token = await _um.GeneratePasswordResetTokenAsync(user);
        var otp = Random.Shared.Next(100000, 999999).ToString();
        var link = $"/reset-password?email={Uri.EscapeDataString(dto.Email)}&token={Uri.EscapeDataString(token)}";

        await _email.SendPasswordResetAsync(dto.Email, otp, link);
        await _audit.LogAsync(user.Id, "PasswordResetRequested");
        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest dto)
    {
        if (dto.NewPassword != dto.ConfirmNewPassword)
            return Result.Failure("Passwords do not match.");

        var user = await _um.FindByEmailAsync(dto.Email);
        if (user == null || user.Status != "Active")
            return Result.Failure("Invalid request.", 400);

        var result = await _um.ResetPasswordAsync(user, dto.Token, dto.NewPassword);
        if (!result.Succeeded)
            return Result.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));

        await _audit.LogAsync(user.Id, "PasswordResetCompleted");
        return Result.Success();
    }

    public async Task<Result<UserSession>> GetSessionAsync(int userId)
    {
        var user = await _um.FindByIdAsync(userId.ToString());
        if (user == null) return Result<UserSession>.Failure("Not found.", 404);

        var roles = await _um.GetRolesAsync(user);
        var permissions = await _perms.GetEffectivePermissionsAsync(userId);

        return Result<UserSession>.Success(new UserSession(
            user.Id, user.Name ?? user.Email!, user.Email!,
            user.Mobile, user.Photo, user.Status,
            user.ExpirationDate, roles.ToList(), permissions));
    }

    private static IEnumerable<Claim> BuildClaims(ApplicationUser u, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, u.Id.ToString()),
            new(ClaimTypes.Email, u.Email!),
            new(ClaimTypes.Name, u.Name ?? u.Email!),
            new("status", u.Status),
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        return claims;
    }
}