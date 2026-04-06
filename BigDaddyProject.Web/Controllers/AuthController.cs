using BigDaddyProject.Application.DTOs;
using BigDaddyProject.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BigDaddyProject.Web.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("login"), AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var result = await _auth.LoginAsync(req);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { error = result.Error });

        if (result.Value!.RequiresDeviceConfirmation)
            return Conflict(new { requiresDeviceConfirmation = true, message = result.Value.DeviceMessage });

        // Set cookie so Blazor SSR can read JWT server-side
        Response.Cookies.Append("access_token", result.Value.Token, new CookieOptions
        {
            HttpOnly = false,  // false so JS can read it for sessionStorage sync
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = result.Value.ExpiresAt
        });

        return Ok(result.Value);
    }

    [HttpPost("logout"), Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _auth.LogoutAsync(userId);
        Response.Cookies.Delete("access_token");
        return Ok(new { message = "Logged out." });
    }

    [HttpPost("change-password"), Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var r = await _auth.ChangePasswordAsync(userId, req);
        return r.IsSuccess ? Ok() : BadRequest(new { error = r.Error });
    }

    [HttpPost("forgot-password"), AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req)
    {
        await _auth.ForgotPasswordAsync(req);
        return Ok(new { message = "If the email exists, instructions have been sent." });
    }

    [HttpPost("reset-password"), AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
    {
        var r = await _auth.ResetPasswordAsync(req);
        return r.IsSuccess ? Ok() : BadRequest(new { error = r.Error });
    }

    [HttpGet("me"), Authorize]
    public async Task<IActionResult> Me()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var r = await _auth.GetSessionAsync(userId);
        return r.IsSuccess ? Ok(r.Value) : NotFound();
    }
}
