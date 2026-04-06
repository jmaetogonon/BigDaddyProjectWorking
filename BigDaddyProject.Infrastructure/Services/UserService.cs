using BigDaddyProject.Application.DTOs;
using BigDaddyProject.Application.Interfaces;
using BigDaddyProject.Domain.Common;
using BigDaddyProject.Domain.Entities;
using BigDaddyProject.Infrastructure.Data;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace BigDaddyProject.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _um;
    private readonly IAuditService _audit;
    private readonly IEmailService _email;
    private readonly AppDbContext _db;

    public UserService(UserManager<ApplicationUser> um, IAuditService audit,
        IEmailService email, AppDbContext db)
    {
        _um = um; _audit = audit; _email = email; _db = db;
    }

    public async Task<Result<PagedResult<UserListItem>>> GetUsersAsync(UserListQuery q)
    {
        var query = _db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Name))
            query = query.Where(u => u.Name!.Contains(q.Name));
        if (!string.IsNullOrWhiteSpace(q.Email))
            query = query.Where(u => u.Email!.Contains(q.Email));
        if (!string.IsNullOrWhiteSpace(q.RegNum))
            query = query.Where(u => u.CEANumber!.Contains(q.RegNum));
        if (!string.IsNullOrWhiteSpace(q.Mobile))
            query = query.Where(u => u.Mobile!.Contains(q.Mobile));
        if (!string.IsNullOrWhiteSpace(q.Status) && q.Status != "All")
            query = query.Where(u => u.Status == q.Status);

        var total = await query.CountAsync();
        var users = await query.OrderBy(u => u.Name)
            .Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)
            .ToListAsync();

        var items = new List<UserListItem>();
        foreach (var u in users)
        {
            var roles = await _um.GetRolesAsync(u);
            var teams = await _db.AgentTeams.Where(at => at.UserId == u.Id)
                .Select(at => at.Team.Name).ToListAsync();
            var deviceCount = await _db.UserDevices.CountAsync(d => d.UserId == u.Id && d.IsActive);
            var lastLogin = await _db.UserDevices
                .Where(d => d.UserId == u.Id)
                .MaxAsync(d => (DateTime?)d.LastLogin);

            items.Add(new UserListItem
            {
                Id = u.Id,
                Name = u.Name ?? u.Email!,
                Email = u.Email!,
                Mobile = u.Mobile,
                RegNumber = u.CEANumber,
                Status = u.Status,
                MultiTerminal = deviceCount > 1,
                ExpirationDate = u.ExpirationDate,
                LastLoginTime = lastLogin,
                Teams = teams,
                Roles = roles.ToList()
            });
        }

        return Result<PagedResult<UserListItem>>.Success(new PagedResult<UserListItem>
        {
            Items = items,
            TotalCount = total,
            Page = q.Page,
            PageSize = q.PageSize
        });
    }

    public async Task<Result<UserListItem>> GetUserByIdAsync(int userId)
    {
        var u = await _db.Users.FindAsync(userId);
        if (u == null) return Result<UserListItem>.Failure("User not found.", 404);

        var roles = await _um.GetRolesAsync(u);
        var teams = await _db.AgentTeams.Where(at => at.UserId == u.Id)
            .Select(at => at.Team.Name).ToListAsync();

        return Result<UserListItem>.Success(new UserListItem
        {
            Id = u.Id,
            Name = u.Name ?? u.Email!,
            Email = u.Email!,
            Mobile = u.Mobile,
            RegNumber = u.CEANumber,
            Status = u.Status,
            ExpirationDate = u.ExpirationDate,
            Teams = teams,
            Roles = roles.ToList()
        });
    }

    public async Task<Result<int>> CreateUserAsync(CreateUserRequest dto, int createdBy)
    {
        if (await _um.FindByEmailAsync(dto.Email) != null)
            return Result<int>.Failure("Email already exists.");

        if (!string.IsNullOrWhiteSpace(dto.CEANumber) &&
            await _db.Users.AnyAsync(u => u.CEANumber == dto.CEANumber))
            return Result<int>.Failure("CEA Number already exists.");

        var tempPassword = GenerateTempPassword();
        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            Name = dto.Name,
            CEANumber = dto.CEANumber,
            CEAExpiry = dto.CEAExpiry,
            Mobile = dto.Mobile,
            ExpirationDate = dto.ExpirationDate,
            Status = dto.Status,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = createdBy,
            EmailConfirmed = true,
            MustChangePassword = true
        };

        var result = await _um.CreateAsync(user, tempPassword);
        if (!result.Succeeded)
            return Result<int>.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));

        await _um.AddToRoleAsync(user, "End User");
        await _email.SendWelcomeAsync(dto.Email, dto.Name, tempPassword);
        await _audit.LogAsync(user.Id, "UserCreated", $"Created by {createdBy}", createdBy);

        return Result<int>.Success(user.Id);
    }

    public async Task<Result> UpdateUserAsync(int userId, UpdateUserRequest dto)
    {
        var user = await _um.FindByIdAsync(userId.ToString());
        if (user == null) return Result.Failure("User not found.", 404);

        if (dto.Name != null) user.Name = dto.Name;
        if (dto.Mobile != null) user.Mobile = dto.Mobile;
        if (dto.ExpirationDate != null) user.ExpirationDate = dto.ExpirationDate;
        if (dto.Status != null) user.Status = dto.Status;
        if (dto.CEANumber != null) user.CEANumber = dto.CEANumber;
        if (dto.CEAExpiry != null) user.CEAExpiry = dto.CEAExpiry;
        if (dto.Email != null && dto.Email != user.Email)
        {
            user.Email = dto.Email;
            user.UserName = dto.Email;
        }
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _um.UpdateAsync(user);
        if (!result.Succeeded)
            return Result.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));

        await _audit.LogAsync(userId, "UserUpdated");
        return Result.Success();
    }

    public async Task<Result> ActivateAsync(int userId, int adminId)
    {
        var user = await _um.FindByIdAsync(userId.ToString());
        if (user == null) return Result.Failure("Not found.", 404);
        user.Status = "Active"; user.UpdatedAt = DateTime.UtcNow;
        await _um.UpdateAsync(user);
        await _audit.LogAsync(userId, "UserActivated", performedBy: adminId);
        return Result.Success();
    }

    public async Task<Result> DeactivateAsync(int userId, int adminId)
    {
        var user = await _um.FindByIdAsync(userId.ToString());
        if (user == null) return Result.Failure("Not found.", 404);
        user.Status = "Inactive"; user.UpdatedAt = DateTime.UtcNow;
        await _um.UpdateAsync(user);
        await _db.RefreshTokens.Where(r => r.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.IsRevoked, true));
        await _audit.LogAsync(userId, "UserDeactivated", performedBy: adminId);
        return Result.Success();
    }

    public async Task<Result<string>> AdminResetPasswordAsync(int userId, int adminId)
    {
        var user = await _um.FindByIdAsync(userId.ToString());
        if (user == null) return Result<string>.Failure("Not found.", 404);

        var token = await _um.GeneratePasswordResetTokenAsync(user);
        var temp = GenerateTempPassword();
        var result = await _um.ResetPasswordAsync(user, token, temp);
        if (!result.Succeeded)
            return Result<string>.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));

        user.MustChangePassword = true;
        await _um.UpdateAsync(user);
        await _email.SendWelcomeAsync(user.Email!, user.Name ?? user.Email!, temp);
        await _audit.LogAsync(userId, "AdminPasswordReset", performedBy: adminId);
        return Result<string>.Success(temp);
    }

    public async Task<Result> BulkCreateAsync(Stream csvStream, int createdBy)
    {
        using var reader = new StreamReader(csvStream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));
        var records = csv.GetRecords<CreateUserRequest>().ToList();
        foreach (var r in records)
            await CreateUserAsync(r, createdBy);
        return Result.Success();
    }

    public async Task<Result> AssignTeamsAsync(int userId, List<int> teamIds)
    {
        var existing = _db.AgentTeams.Where(at => at.UserId == userId);
        _db.AgentTeams.RemoveRange(existing);
        foreach (var tid in teamIds)
            _db.AgentTeams.Add(new AgentTeam { UserId = userId, TeamId = tid });
        await _db.SaveChangesAsync();
        await _audit.LogAsync(userId, "TeamsAssigned", string.Join(",", teamIds));
        return Result.Success();
    }

    private static string GenerateTempPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#$";
        var rng = new Random();
        return new string(Enumerable.Repeat(chars, 12).Select(s => s[rng.Next(s.Length)]).ToArray())
               + "A1!";  // ensures password policy is met
    }
}