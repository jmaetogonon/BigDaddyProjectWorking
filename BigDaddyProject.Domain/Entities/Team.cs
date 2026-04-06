namespace BigDaddyProject.Domain.Entities;

public class Team
{
    public int Id { get; set; }
    public string ProjectId { get; set; } = "DEFAULT";
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<AgentTeam> AgentTeams { get; set; } = new List<AgentTeam>();
    public ICollection<TeamRole> TeamRoles { get; set; } = new List<TeamRole>();
}
