using BigDaddyProject.Application.DTOs;
using BigDaddyProject.Application.Interfaces;
using BigDaddyProject.Domain.Common;
using BigDaddyProject.Domain.Entities;
using BigDaddyProject.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BigDaddyProject.Infrastructure.Services;

public class TeamService : ITeamService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;
    public TeamService(AppDbContext db, IAuditService audit) { _db = db; _audit = audit; }

    public async Task<Result<int>> CreateTeamAsync(CreateTeamRequest dto, int createdBy)
    {
        if (await _db.Teams.AnyAsync(t => t.Name == dto.Name))
            return Result<int>.Failure("Team name already exists.");
        var team = new Team { Name = dto.Name };
        _db.Teams.Add(team);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(createdBy, "TeamCreated", dto.Name, createdBy);
        return Result<int>.Success(team.Id);
    }

    public async Task<Result> UpdateTeamAsync(int teamId, CreateTeamRequest dto)
    {
        var team = await _db.Teams.FindAsync(teamId);
        if (team == null) return Result.Failure("Not found.", 404);
        team.Name = dto.Name;
        await _db.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> AssignUsersAsync(int teamId, List<int> userIds)
    {
        var existing = _db.AgentTeams.Where(at => at.TeamId == teamId);
        _db.AgentTeams.RemoveRange(existing);
        foreach (var uid in userIds)
            _db.AgentTeams.Add(new AgentTeam { UserId = uid, TeamId = teamId });
        await _db.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> AssignRolesAsync(int teamId, List<int> roleIds)
    {
        var existing = _db.TeamRoles.Where(tr => tr.TeamId == teamId);
        _db.TeamRoles.RemoveRange(existing);
        foreach (var rid in roleIds)
            _db.TeamRoles.Add(new TeamRole { TeamId = teamId, RoleId = rid });
        await _db.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<List<TeamDto>>> GetTeamsAsync()
    {
        var teams = await _db.Teams
            .Include(t => t.AgentTeams)
            .Include(t => t.TeamRoles).ThenInclude(tr => tr.Role)
            .ToListAsync();

        return Result<List<TeamDto>>.Success(teams.Select(t => new TeamDto
        {
            Id = t.Id,
            Name = t.Name,
            MemberCount = t.AgentTeams.Count,
            Roles = t.TeamRoles.Select(tr => tr.Role.Name!).ToList()
        }).ToList());
    }

    public async Task<Result<TeamDto>> GetTeamByIdAsync(int teamId)
    {
        var t = await _db.Teams
            .Include(x => x.AgentTeams)
            .Include(x => x.TeamRoles).ThenInclude(tr => tr.Role)
            .FirstOrDefaultAsync(x => x.Id == teamId);
        if (t == null) return Result<TeamDto>.Failure("Not found.", 404);
        return Result<TeamDto>.Success(new TeamDto
        {
            Id = t.Id,
            Name = t.Name,
            MemberCount = t.AgentTeams.Count,
            Roles = t.TeamRoles.Select(tr => tr.Role.Name!).ToList()
        });
    }
}
