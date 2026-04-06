using BigDaddyProject.Application.DTOs;
using BigDaddyProject.Domain.Common;

namespace BigDaddyProject.Application.Interfaces;

public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request);
    Task<Result> LogoutAsync(int userId);
    Task<Result> ChangePasswordAsync(int userId, ChangePasswordRequest dto);
    Task<Result> ForgotPasswordAsync(ForgotPasswordRequest dto);
    Task<Result> ResetPasswordAsync(ResetPasswordRequest dto);
    Task<Result<UserSession>> GetSessionAsync(int userId);
}

public interface IUserService
{
    Task<Result<int>> CreateUserAsync(CreateUserRequest dto, int createdBy);
    Task<Result> UpdateUserAsync(int userId, UpdateUserRequest dto);
    Task<Result> ActivateAsync(int userId, int adminId);
    Task<Result> DeactivateAsync(int userId, int adminId);
    Task<Result<string>> AdminResetPasswordAsync(int userId, int adminId);
    Task<Result<PagedResult<UserListItem>>> GetUsersAsync(UserListQuery query);
    Task<Result<UserListItem>> GetUserByIdAsync(int userId);
    Task<Result> BulkCreateAsync(Stream csvStream, int createdBy);
    Task<Result> AssignTeamsAsync(int userId, List<int> teamIds);
}

public interface ITeamService
{
    Task<Result<int>> CreateTeamAsync(CreateTeamRequest dto, int createdBy);
    Task<Result> UpdateTeamAsync(int teamId, CreateTeamRequest dto);
    Task<Result> AssignUsersAsync(int teamId, List<int> userIds);
    Task<Result> AssignRolesAsync(int teamId, List<int> roleIds);
    Task<Result<List<TeamDto>>> GetTeamsAsync();
    Task<Result<TeamDto>> GetTeamByIdAsync(int teamId);
}

public interface IRoleService
{
    Task<Result<int>> CreateRoleAsync(CreateRoleRequest dto, int createdBy);
    Task<Result> UpdateRoleAsync(int roleId, CreateRoleRequest dto);
    Task<Result> AssignPermissionsAsync(int roleId, AssignPermissionsToRoleRequest request);
    Task<Result<List<RoleDto>>> GetRolesAsync();
    Task<Result<RoleDto>> GetRoleByIdAsync(int roleId);
}

public interface IPermissionService
{
    Task<Dictionary<string, int>> GetEffectivePermissionsAsync(int userId);
    Task<bool> HasPermissionAsync(int userId, string permissionName, int minimumLevel = 1);
    Task<Result<List<PermissionDto>>> GetAllPermissionsAsync();
}

public interface ITokenService
{
    string GenerateAccessToken(IEnumerable<System.Security.Claims.Claim> claims);
    string GenerateRefreshToken();
    System.Security.Claims.ClaimsPrincipal? ValidateToken(string token);
    DateTime GetTokenExpiry();
}

public interface IAuditService
{
    Task LogAsync(int userId, string operation, string? details = null,
                  int? performedBy = null, string? ipAddress = null);
}

public interface IEmailService
{
    Task SendPasswordResetAsync(string toEmail, string otp, string resetLink);
    Task SendWelcomeAsync(string toEmail, string name, string tempPassword);
}