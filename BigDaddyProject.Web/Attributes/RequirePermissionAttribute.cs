using BigDaddyProject.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace BigDaddyProject.Web.Attributes;

/// <summary>
/// Fine-grained permission check for API controllers.
/// Use alongside [Authorize] — JWT must be valid first, then this checks the permission level.
///
/// Example:
///   [Authorize(Roles = "System Administrator,Manager")]
///   [RequirePermission("Manage Transaction", minLevel: 2)]  // 2 = Organization
///   public async Task<IActionResult> CreateUser(...) { }
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequirePermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _permission;
    private readonly int _minLevel;

    public RequirePermissionAttribute(string permission, int minLevel = 1)
    { _permission = permission; _minLevel = minLevel; }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext ctx)
    {
        var userId = ctx.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            ctx.Result = new UnauthorizedObjectResult(new { error = "Not authenticated." });
            return;
        }

        var svc = ctx.HttpContext.RequestServices.GetRequiredService<IPermissionService>();
        var ok = await svc.HasPermissionAsync(int.Parse(userId), _permission, _minLevel);
        if (!ok)
            ctx.Result = new ObjectResult(new { error = $"Permission required: {_permission}" })
            { StatusCode = 403 };
    }
}