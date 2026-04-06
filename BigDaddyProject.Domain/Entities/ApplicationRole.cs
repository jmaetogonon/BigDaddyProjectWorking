using Microsoft.AspNetCore.Identity;

namespace BigDaddyProject.Domain.Entities;

public class ApplicationRole : IdentityRole<int>
{
    public string ProjectId { get; set; } = "DEFAULT";
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TeamRole> TeamRoles { get; set; } = new List<TeamRole>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
