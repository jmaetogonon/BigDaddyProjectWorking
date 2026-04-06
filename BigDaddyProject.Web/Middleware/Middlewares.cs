using BigDaddyProject.Application.Interfaces;
using System.Security.Claims;

namespace BigDaddyProject.Web.Middleware;

/// <summary>
/// Reads JWT from Authorization header (API) or access_token cookie (Blazor SSR).
/// Runs before UseAuthentication so HttpContext.User is populated for both flows.
/// </summary>
public class JwtMiddleware
{
    private readonly RequestDelegate _next;
    public JwtMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx, ITokenService tokenSvc)
    {
        var token = ctx.Request.Headers.Authorization.FirstOrDefault()?.Split(" ").Last();
        if (string.IsNullOrEmpty(token))
            token = ctx.Request.Cookies["access_token"];

        if (!string.IsNullOrEmpty(token))
        {
            var principal = tokenSvc.ValidateToken(token);
            if (principal != null)
                ctx.User = principal;
        }

        await _next(ctx);
    }
}

/// <summary>
/// ICurrentUser — inject in Blazor components or services to get current user
/// without needing IHttpContextAccessor directly.
/// </summary>
public interface ICurrentUser
{
    int? UserId { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
    bool IsManager { get; }
    Task<bool> HasPermissionAsync(string permissionName, int minLevel = 1);
}

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _hca;
    private readonly IPermissionService _perms;
    private Dictionary<string, int>? _cache;

    public CurrentUser(IHttpContextAccessor hca, IPermissionService perms)
    { _hca = hca; _perms = perms; }

    private ClaimsPrincipal? User => _hca.HttpContext?.User;

    public int? UserId => int.TryParse(
        User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public string? UserName => User?.FindFirstValue(ClaimTypes.Name);
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;
    public bool IsAdmin => User?.IsInRole("System Administrator") == true;
    public bool IsManager => User?.IsInRole("Manager") == true || IsAdmin;

    public async Task<bool> HasPermissionAsync(string permissionName, int minLevel = 1)
    {
        if (!IsAuthenticated || UserId == null) return false;
        _cache ??= await _perms.GetEffectivePermissionsAsync(UserId.Value);
        return _cache.TryGetValue(permissionName, out var lvl) && lvl >= minLevel;
    }
}

/// <summary>
/// Blocks /admin/** routes for unauthenticated users or users without admin/manager role.
/// Redirects to /login or /unauthorized automatically.
/// </summary>
public class AdminPortalGuardMiddleware
{
    private readonly RequestDelegate _next;
    public AdminPortalGuardMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx)
    {
        var path = ctx.Request.Path.Value ?? "";

        if (path.StartsWith("/admin", StringComparison.OrdinalIgnoreCase))
        {
            var user = ctx.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                ctx.Response.Redirect("/login?returnUrl=" + Uri.EscapeDataString(path));
                return;
            }
            if (!user.IsInRole("System Administrator") && !user.IsInRole("Manager"))
            {
                ctx.Response.Redirect("/unauthorized");
                return;
            }
        }

        await _next(ctx);
    }
}