namespace BigDaddyProject.Domain.Entities;

public class RolePermission
{
    public int RoleId { get; set; }
    public int PermissionId { get; set; }
    public int AccessLevel { get; set; }   // 0=None, 1=Individual, 2=Organization
    public ApplicationRole Role { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}
