using Microsoft.AspNetCore.Identity;

namespace BigDaddyProject.Domain.Entities;

// Identity uses int as primary key (not GUID)
public class ApplicationUser : IdentityUser<int>
{
    public string? Name { get; set; }
    public string? NRICName { get; set; }
    public string? CEANumber { get; set; }
    public DateTime? CEAExpiry { get; set; }
    public string? Mobile { get; set; }
    public string? Gender { get; set; }
    public string? Photo { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime? ExpirationDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public int? CreatedByUserId { get; set; }
    public bool MustChangePassword { get; set; } = false;

    public ICollection<AgentTeam> AgentTeams { get; set; } = new List<AgentTeam>();
    public ICollection<UserDevice> UserDevices { get; set; } = new List<UserDevice>();
    public ICollection<UserAuditLog> AuditLogs { get; set; } = new List<UserAuditLog>();
}
