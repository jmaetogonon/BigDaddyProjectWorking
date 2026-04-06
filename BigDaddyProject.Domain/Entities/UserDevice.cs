namespace BigDaddyProject.Domain.Entities;

public class UserDevice
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;  // Phone | Tablet | Web
    public string? Platform { get; set; }
    public string? DeviceName { get; set; }
    public string? AppVersion { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLogin { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ApplicationUser User { get; set; } = null!;
}
