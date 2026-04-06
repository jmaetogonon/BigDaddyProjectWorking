using BigDaddyProject.Application.DTOs;
using BigDaddyProject.Application.Interfaces;
using BigDaddyProject.Web.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BigDaddyProject.Web.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "System Administrator,Manager")]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;
    public UsersController(IUserService users) => _users = users;

    [HttpGet]
    [RequirePermission("Manage Transaction", minLevel: 2)]
    public async Task<IActionResult> List([FromQuery] UserListQuery q)
        => Ok((await _users.GetUsersAsync(q)).Value);

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var r = await _users.GetUserByIdAsync(id);
        return r.IsSuccess ? Ok(r.Value) : NotFound();
    }

    [HttpPost]
    [RequirePermission("Project Data Administrator", 2)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest req)
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var r = await _users.CreateUserAsync(req, adminId);
        return r.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = r.Value }, new { id = r.Value })
            : BadRequest(new { error = r.Error });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest req)
    {
        var r = await _users.UpdateUserAsync(id, req);
        return r.IsSuccess ? NoContent() : BadRequest(new { error = r.Error });
    }

    [HttpPost("{id:int}/activate")]
    public async Task<IActionResult> Activate(int id)
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var r = await _users.ActivateAsync(id, adminId);
        return r.IsSuccess ? Ok() : BadRequest(new { error = r.Error });
    }

    [HttpPost("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var r = await _users.DeactivateAsync(id, adminId);
        return r.IsSuccess ? Ok() : BadRequest(new { error = r.Error });
    }

    [HttpPost("{id:int}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id)
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var r = await _users.AdminResetPasswordAsync(id, adminId);
        return r.IsSuccess ? Ok(new { tempPassword = r.Value }) : BadRequest(new { error = r.Error });
    }

    [HttpPost("{id:int}/teams")]
    public async Task<IActionResult> AssignTeams(int id, [FromBody] List<int> teamIds)
    {
        var r = await _users.AssignTeamsAsync(id, teamIds);
        return r.IsSuccess ? Ok() : BadRequest(new { error = r.Error });
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> BulkCreate(IFormFile file)
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        using var stream = file.OpenReadStream();
        var r = await _users.BulkCreateAsync(stream, adminId);
        return r.IsSuccess ? Ok() : BadRequest(new { error = r.Error });
    }
}