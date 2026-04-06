using BigDaddyProject.Application.DTOs;
using Microsoft.JSInterop;
using System.Text.Json;

namespace BigDaddyProject.Web.Services;

/// <summary>
/// Wraps browser sessionStorage — which is ISOLATED PER TAB.
///
/// Behavior:
///   Tab A logs in  → sessionStorage["userInfo"] = {...}  → authenticated
///   Tab B opens    → sessionStorage is EMPTY             → must log in again
///   Tab A refreshes→ sessionStorage still has data       → still authenticated
///   Tab A logs out → sessionStorage cleared              → Tab B unaffected
/// </summary>
public class SessionStorageService
{
    private readonly IJSRuntime _js;
    private const string KEY = "userInfo";

    public SessionStorageService(IJSRuntime js) => _js = js;

    public async Task SetAsync(UserSession session, string token, string refreshToken, DateTime expiresAt)
    {
        var info = new SessionInfo
        {
            UserId = session.UserId,
            UserName = session.UserName,
            Email = session.Email,
            Mobile = session.Mobile,
            Photo = session.Photo,
            Status = session.Status,
            ExpirationDate = session.ExpirationDate,
            Token = token,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            Roles = session.Roles,
            Permissions = session.Permissions
        };

        var json = JsonSerializer.Serialize(info);
        await _js.InvokeVoidAsync("sessionStorage.setItem", KEY, json);

        // Also set cookie so Blazor SSR server-side can validate JWT
        await _js.InvokeVoidAsync("bigdaddy.setCookie",
            "access_token", token,
            (int)(expiresAt - DateTime.UtcNow).TotalMinutes);
    }

    public async Task<SessionInfo?> GetAsync()
    {
        try
        {
            var json = await _js.InvokeAsync<string?>("sessionStorage.getItem", KEY);
            if (string.IsNullOrEmpty(json)) return null;
            var info = JsonSerializer.Deserialize<SessionInfo>(json);
            if (info == null || info.ExpiresAt <= DateTime.UtcNow) return null;
            return info;
        }
        catch { return null; }
    }

    public async Task ClearAsync()
    {
        await _js.InvokeVoidAsync("sessionStorage.removeItem", KEY);
        await _js.InvokeVoidAsync("bigdaddy.removeCookie", "access_token");
    }

    public async Task<bool> IsAuthenticatedAsync() => await GetAsync() != null;
}

public class SessionInfo
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string? Photo { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ExpirationDate { get; set; }
    public string Token { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public List<string> Roles { get; set; } = new();
    public Dictionary<string, int> Permissions { get; set; } = new();

    public bool IsAdmin => Roles.Contains("System Administrator");
    public bool IsManager => Roles.Contains("Manager") || IsAdmin;
    public bool HasPermission(string name, int min = 1) =>
        Permissions.TryGetValue(name, out var lvl) && lvl >= min;
}