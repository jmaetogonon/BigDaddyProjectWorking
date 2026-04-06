using BigDaddyProject.Application.DTOs;
using BigDaddyProject.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BigDaddyProject.Web.Controllers;

[ApiController]
[Route("api/teams")]
[Authorize(Roles = "System Administrator,Manager")]
public class TeamsController : ControllerBase
{
    private readonly ITeamService _teams;
    public TeamsController(ITeamService teams) => _teams = teams;

    [HttpGet] public async Task<IActionResult> List() => Ok((await _teams.GetTeamsAsync()).Value);

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var r = await _teams.GetTeamByIdAsync(id);
        return r.IsSuccess ? Ok(r.Value) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTeamRequest req)
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var r = await _teams.CreateTeamAsync(req, adminId);
        return r.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = r.Value }, new { id = r.Value })
            : BadRequest(new { error = r.Error });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateTeamRequest req)
    {
        var r = await _teams.UpdateTeamAsync(id, req);
        return r.IsSuccess ? NoContent() : BadRequest(new { error = r.Error });
    }

    [HttpPost("{id:int}/users")]
    public async Task<IActionResult> AssignUsers(int id, [FromBody] List<int> userIds)
        => (await _teams.AssignUsersAsync(id, userIds)).IsSuccess ? Ok() : BadRequest();

    [HttpPost("{id:int}/roles")]
    public async Task<IActionResult> AssignRoles(int id, [FromBody] List<int> roleIds)
        => (await _teams.AssignRolesAsync(id, roleIds)).IsSuccess ? Ok() : BadRequest();
}