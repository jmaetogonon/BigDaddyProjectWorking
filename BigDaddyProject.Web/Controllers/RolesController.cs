using BigDaddyProject.Application.DTOs;
using BigDaddyProject.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BigDaddyProject.Web.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize(Roles = "System Administrator")]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roles;
    private readonly IPermissionService _perms;
    public RolesController(IRoleService roles, IPermissionService perms)
    { _roles = roles; _perms = perms; }

    [HttpGet] public async Task<IActionResult> List() => Ok((await _roles.GetRolesAsync()).Value);

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var r = await _roles.GetRoleByIdAsync(id);
        return r.IsSuccess ? Ok(r.Value) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest req)
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var r = await _roles.CreateRoleAsync(req, adminId);
        return r.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = r.Value }, new { id = r.Value })
            : BadRequest(new { error = r.Error });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateRoleRequest req)
    {
        var r = await _roles.UpdateRoleAsync(id, req);
        return r.IsSuccess ? NoContent() : BadRequest(new { error = r.Error });
    }

    [HttpPost("{id:int}/permissions")]
    public async Task<IActionResult> AssignPermissions(int id,
        [FromBody] AssignPermissionsToRoleRequest req)
    {
        var r = await _roles.AssignPermissionsAsync(id, req);
        return r.IsSuccess ? Ok() : BadRequest(new { error = r.Error });
    }

    [HttpGet("permissions")]
    public async Task<IActionResult> AllPermissions()
        => Ok((await _perms.GetAllPermissionsAsync()).Value);
}