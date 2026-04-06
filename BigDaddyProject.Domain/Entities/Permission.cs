namespace BigDaddyProject.Domain.Entities;

public class Permission
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;   // Organization | Property
    public string Group { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string Levels { get; set; } = "0,1,2";

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
