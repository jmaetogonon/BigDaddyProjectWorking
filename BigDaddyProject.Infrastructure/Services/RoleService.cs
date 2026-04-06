using BigDaddyProject.Application.DTOs;
using BigDaddyProject.Application.Interfaces;
using BigDaddyProject.Domain.Common;
using BigDaddyProject.Domain.Entities;
using BigDaddyProject.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BigDaddyProject.Infrastructure.Services;

public class RoleService : IRoleService
{
    private readonly AppDbContext _db;
    private readonly RoleManager<ApplicationRole> _rm;
    private readonly IAuditService _audit;

    public RoleService(AppDbContext db, RoleManager<ApplicationRole> rm, IAuditService audit)
    { _db = db; _rm = rm; _audit = audit; }

    public async Task<Result<int>> CreateRoleAsync(CreateRoleRequest dto, int createdBy)
    {
        if (await _rm.RoleExistsAsync(dto.Name))
            return Result<int>.Failure("Role already exists.");
        var role = new ApplicationRole { Name = dto.Name, Description = dto.Description };
        await _rm.CreateAsync(role);
        await _audit.LogAsync(createdBy, "RoleCreated", dto.Name, createdBy);
        return Result<int>.Success(role.Id);
    }

    public async Task<Result> UpdateRoleAsync(int roleId, CreateRoleRequest dto)
    {
        var role = await _rm.FindByIdAsync(roleId.ToString());
        if (role == null) return Result.Failure("Not found.", 404);
        role.Name = dto.Name; role.Description = dto.Description;
        await _rm.UpdateAsync(role);
        return Result.Success();
    }

    public async Task<Result> AssignPermissionsAsync(int roleId, AssignPermissionsToRoleRequest request)
    {
        var existing = _db.RolePermissions.Where(rp => rp.RoleId == roleId);
        _db.RolePermissions.RemoveRange(existing);
        foreach (var a in request.Assignments)
            _db.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = a.PermissionId,
                AccessLevel = a.AccessLevel
            });
        await _db.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<List<RoleDto>>> GetRolesAsync()
    {
        var roles = await _db.Roles
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .ToListAsync();
        return Result<List<RoleDto>>.Success(roles.Select(r => new RoleDto
        {
            Id = r.Id,
            Name = r.Name!,
            Description = r.Description,
            Permissions = r.RolePermissions.Select(rp => rp.Permission.Name).ToList()
        }).ToList());
    }

    public async Task<Result<RoleDto>> GetRoleByIdAsync(int roleId)
    {
        var r = await _db.Roles
            .Include(x => x.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(x => x.Id == roleId);
        if (r == null) return Result<RoleDto>.Failure("Not found.", 404);
        return Result<RoleDto>.Success(new RoleDto
        {
            Id = r.Id,
            Name = r.Name!,
            Description = r.Description,
            Permissions = r.RolePermissions.Select(rp => rp.Permission.Name).ToList()
        });
    }
}