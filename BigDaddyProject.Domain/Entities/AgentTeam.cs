namespace BigDaddyProject.Domain.Entities;

public class AgentTeam
{
    public int UserId { get; set; }
    public int TeamId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public Team Team { get; set; } = null!;
}
