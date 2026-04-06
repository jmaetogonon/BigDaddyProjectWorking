using BigDaddyProject.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BigDaddyProject.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<
   ApplicationUser, ApplicationRole, int,
   IdentityUserClaim<int>, IdentityUserRole<int>, IdentityUserLogin<int>,
   IdentityRoleClaim<int>, IdentityUserToken<int>>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Team> Teams => Set<Team>();
    public DbSet<AgentTeam> AgentTeams => Set<AgentTeam>();
    public DbSet<TeamRole> TeamRoles => Set<TeamRole>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserDevice> UserDevices => Set<UserDevice>();
    public DbSet<UserAuditLog> UserAuditLogs => Set<UserAuditLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // AgentTeam — composite PK (UserId, TeamId)
        b.Entity<AgentTeam>().HasKey(x => new { x.UserId, x.TeamId });
        b.Entity<AgentTeam>().HasOne(x => x.User).WithMany(u => u.AgentTeams).HasForeignKey(x => x.UserId);
        b.Entity<AgentTeam>().HasOne(x => x.Team).WithMany(t => t.AgentTeams).HasForeignKey(x => x.TeamId);

        // TeamRole — composite PK (TeamId, RoleId)
        b.Entity<TeamRole>().HasKey(x => new { x.TeamId, x.RoleId });
        b.Entity<TeamRole>().HasOne(x => x.Team).WithMany(t => t.TeamRoles).HasForeignKey(x => x.TeamId);
        b.Entity<TeamRole>().HasOne(x => x.Role).WithMany(r => r.TeamRoles).HasForeignKey(x => x.RoleId);

        // RolePermission — composite PK (RoleId, PermissionId)
        b.Entity<RolePermission>().HasKey(x => new { x.RoleId, x.PermissionId });
        b.Entity<RolePermission>().HasOne(x => x.Role).WithMany(r => r.RolePermissions).HasForeignKey(x => x.RoleId);
        b.Entity<RolePermission>().HasOne(x => x.Permission).WithMany(p => p.RolePermissions).HasForeignKey(x => x.PermissionId);

        b.Entity<ApplicationUser>(e =>
        {
            e.Property(u => u.Name).HasMaxLength(250);
            e.Property(u => u.NRICName).HasMaxLength(250);
            e.Property(u => u.CEANumber).HasMaxLength(20);
            e.Property(u => u.Mobile).HasMaxLength(20);
            e.Property(u => u.Gender).HasMaxLength(10);
            e.Property(u => u.Photo).HasMaxLength(500);
            e.Property(u => u.Status).HasMaxLength(50).HasDefaultValue("Active");
        });

        b.Entity<Team>(e => e.HasIndex(t => t.Name).IsUnique());

        b.Entity<UserDevice>(e =>
            e.HasOne(d => d.User).WithMany(u => u.UserDevices).HasForeignKey(d => d.UserId));

        b.Entity<UserAuditLog>(e =>
        {
            e.Property(l => l.Operation).HasMaxLength(500);
            e.Property(l => l.Details).HasMaxLength(2000);
            e.HasOne(l => l.User).WithMany(u => u.AuditLogs).HasForeignKey(l => l.UserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<RefreshToken>(e =>
            e.HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId));
    }
}