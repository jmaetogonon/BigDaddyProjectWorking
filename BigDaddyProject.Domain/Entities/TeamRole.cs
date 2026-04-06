namespace BigDaddyProject.Domain.Entities;

public class TeamRole
{
    public int TeamId { get; set; }
    public int RoleId { get; set; }
    public Team Team { get; set; } = null!;
    public ApplicationRole Role { get; set; } = null!;
}
