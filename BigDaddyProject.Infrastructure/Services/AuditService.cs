using BigDaddyProject.Application.Interfaces;
using BigDaddyProject.Infrastructure.Data;

namespace BigDaddyProject.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly AppDbContext _db;
    public AuditService(AppDbContext db) => _db = db;

    public async Task LogAsync(int userId, string operation, string? details = null,
        int? performedBy = null, string? ipAddress = null)
    {
        _db.UserAuditLogs.Add(new Domain.Entities.UserAuditLog
        {
            UserId = userId,
            Operation = operation,
            Details = details,
            PerformedByUserId = performedBy,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}