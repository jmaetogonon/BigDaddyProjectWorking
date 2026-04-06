namespace BigDaddyProject.Domain.Entities;

public class UserAuditLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Operation { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public int? PerformedByUserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
}
