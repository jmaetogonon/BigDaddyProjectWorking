namespace BigDaddyProject.Application.DTOs;

// ── Auth ──────────────────────────────────────────────────────────────
public record LoginRequest(
    string Email,
    string Password,
    string? DeviceId = null,
    string? DeviceType = "Web",
    string? DeviceName = null,
    string? Platform = null,
    string? AppVersion = null,
    bool DeviceConfirmed = false);

public record LoginResponse(
    string Token,
    string RefreshToken,
    DateTime ExpiresAt,
    UserSession UserInfo,
    bool RequiresDeviceConfirmation = false,
    string? DeviceMessage = null);

public record UserSession(
    int UserId,
    string UserName,
    string Email,
    string? Mobile,
    string? Photo,
    string Status,
    DateTime? ExpirationDate,
    List<string> Roles,
    Dictionary<string, int> Permissions)
{
    public bool IsAdmin => Roles.Contains("System Administrator");
    public bool IsManager => Roles.Contains("Manager") || IsAdmin;
    public bool HasPermission(string name, int minLevel = 1) =>
        Permissions.TryGetValue(name, out var lvl) && lvl >= minLevel;
}

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(
    string Email,
    string Token,
    string NewPassword,
    string ConfirmNewPassword);

// ── User Management ───────────────────────────────────────────────────
public record CreateUserRequest(
    string Name,
    string Email,
    string? CEANumber,
    DateTime? CEAExpiry,
    string? Mobile,
    DateTime? ExpirationDate,
    string Status = "Active");

public record UpdateUserRequest(
    string? Name,
    string? Email,
    string? Mobile,
    DateTime? ExpirationDate,
    string? Status,
    string? CEANumber,
    DateTime? CEAExpiry);

public class UserListItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string? RegNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsActive => Status == "Active";
    public bool MultiTerminal { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public DateTime? LastLoginTime { get; set; }
    public List<string> Teams { get; set; } = new();
    public List<string> Roles { get; set; } = new();
}

public class UserListQuery
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? RegNum { get; set; }
    public string? Mobile { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

// ── Teams ─────────────────────────────────────────────────────────────
public record CreateTeamRequest(string Name);
public record AssignUsersToTeamRequest(List<int> UserIds);

public class TeamDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public List<string> Roles { get; set; } = new();
}

// ── Roles ─────────────────────────────────────────────────────────────
public record CreateRoleRequest(string Name, string? Description);

public class RoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Permissions { get; set; } = new();
}

public record AssignPermissionsToRoleRequest(List<PermissionAssignment> Assignments);
public record PermissionAssignment(int PermissionId, int AccessLevel);

// ── Permissions ───────────────────────────────────────────────────────
public class PermissionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}