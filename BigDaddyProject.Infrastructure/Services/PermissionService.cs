using BigDaddyProject.Application.DTOs;
using BigDaddyProject.Application.Interfaces;
using BigDaddyProject.Domain.Common;
using BigDaddyProject.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BigDaddyProject.Infrastructure.Services;

public class PermissionService : IPermissionService
{
    private readonly AppDbContext _db;
    public PermissionService(AppDbContext db) => _db = db;

    /// <summary>
    /// RBAC resolution: User → AgentTeam → TeamRole → RolePermission → Permission.
    /// Highest AccessLevel wins when same permission appears in multiple roles.
    /// </summary>
    public async Task<Dictionary<string, int>> GetEffectivePermissionsAsync(int userId)
    {
        var directRoles = await _db.UserRoles
    .Where(ur => ur.UserId == userId)
    .Select(ur => ur.RoleId)
    .ToListAsync();

        var teamRoles = await _db.AgentTeams
     .Where(at => at.UserId == userId)
     .Join(_db.TeamRoles,
         at => at.TeamId,
         tr => tr.TeamId,
         (_, tr) => tr.RoleId)
     .Distinct()
     .ToListAsync();

        var roleIds = directRoles
            .Union(teamRoles)
            .Distinct()
            .ToList();

        if (!roleIds.Any()) return new();

        var rps = await _db.RolePermissions
            .Include(rp => rp.Permission)
            .Where(rp => roleIds.Contains(rp.RoleId))
            .ToListAsync();

        var effective = new Dictionary<string, int>();
        foreach (var rp in rps)
        {
            var name = rp.Permission.Name;
            if (!effective.TryGetValue(name, out var cur) || rp.AccessLevel > cur)
                effective[name] = rp.AccessLevel;
        }
        return effective;
    }

    public async Task<bool> HasPermissionAsync(int userId, string name, int minLevel = 1)
    {
        var perms = await GetEffectivePermissionsAsync(userId);
        return perms.TryGetValue(name, out var lvl) && lvl >= minLevel;
    }

    public async Task<Result<List<PermissionDto>>> GetAllPermissionsAsync()
    {
        var list = await _db.Permissions
            .OrderBy(p => p.Group).ThenBy(p => p.DisplayOrder)
            .Select(p => new PermissionDto
            {
                Id = p.Id,
                Name = p.Name,
                Type = p.Type,
                Group = p.Group,
                DisplayOrder = p.DisplayOrder
            }).ToListAsync();
        return Result<List<PermissionDto>>.Success(list);
    }
}
