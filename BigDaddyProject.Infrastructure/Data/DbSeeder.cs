using BigDaddyProject.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace BigDaddyProject.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(
        AppDbContext db,
        UserManager<ApplicationUser> um,
        RoleManager<ApplicationRole> rm)
    {
        // Run pending migrations first
        await db.Database.MigrateAsync();

        // 1. Seed roles
        string[] roles = ["System Administrator", "Manager", "End User"];
        foreach (var r in roles)
            if (!await rm.RoleExistsAsync(r))
                await rm.CreateAsync(new ApplicationRole { Name = r });

        // 2. Seed permissions
        var perms = new List<(string Name, string Type, string Group, int Order)>
        {
            ("Project Data Administrator",  "Organization", "General",   1),
            ("Manage Transaction",          "Organization", "General",   2),
            ("Manage Interest",             "Organization", "General",   3),
            ("Manage Notification",         "Organization", "General",   4),
            ("Manage Audit Logs",           "Organization", "General",   5),
            ("Manage Other Selling Entity", "Organization", "General",   6),
            ("View Status Other",           "Organization", "General",   7),
            ("View Summary Other",          "Organization", "General",   8),
            ("Access Project",              "Property", "General",      10),
            ("Edit Project Set",            "Property", "General",      11),
            ("View Price 1 Available Unit", "Property", "Pricing",     20),
            ("View Price 2 Available Unit", "Property", "Pricing",     21),
            ("View Price 3 Available Unit", "Property", "Pricing",     22),
            ("View Status",                 "Property", "Status",      30),
            ("View Status - Reserved",      "Property", "Status",      31),
            ("View Status - SPA Signed",    "Property", "Status",      32),
            ("Change Status - Not Released/Available", "Property", "Status", 33),
            ("View Interest",               "Property", "Interest",    40),
            ("Submit Interest",             "Property", "Interest",    41),
            ("Edit Interest",               "Property", "Interest",    42),
            ("Mark Pending Reserve",        "Property", "Booking",     50),
            ("Mark Reserved",               "Property", "Booking",     51),
            ("Mark Sold",                   "Property", "Booking",     52),
            ("Mark SPA Signed",             "Property", "Booking",     53),
            ("Mark SPA Stamped",            "Property", "Booking",     54),
            ("Edit Pending Reservation",    "Property", "Booking - Edit", 60),
            ("Edit Unit Price",             "Property", "Booking - Edit", 61),
            ("Cancel Reserved",             "Property", "Booking - Cancellation", 70),
            ("Cancel Sold",                 "Property", "Booking - Cancellation", 71),
            ("Generate/Upload Document",    "Property", "Booking - Document Generation", 80),
            ("View Sensitive Document",     "Property", "Booking - Document Generation", 81),
            ("Receive Sold/Available Notification", "Property", "Announcement/Push Message", 90),
            ("Send Announcement",           "Property", "Announcement/Push Message", 91),
            ("View Summary Report",         "Property", "Reports",    100),
            ("Email Reports",               "Property", "Reports",    101),
            ("View Detail Summary Report",  "Property", "Reports",    102),
            ("CMS Access",                  "Property", "CMS",        110),
            ("CMS - Unit Tab Access",       "Property", "CMS",        111),
            ("CMS - Permission Access",     "Property", "CMS",        112),
            ("View Sensitive Files",        "Property", "Data Access",120),
            ("Confirm Progressive Status",  "Property", "Architect/Lawyer", 130),
        };

        foreach (var (name, type, group, order) in perms)
            if (!await db.Permissions.AnyAsync(p => p.Name == name))
                db.Permissions.Add(new Permission
                { Name = name, Type = type, Group = group, DisplayOrder = order });

        await db.SaveChangesAsync();

        // 3. Seed default admin user
        const string adminEmail = "admin@bigdaddy.com";
        if (await um.FindByEmailAsync(adminEmail) == null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                Name = "System Admin",
                Status = "Active",
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };
            var result = await um.CreateAsync(admin, "Admin@123456!");
            if (result.Succeeded)
                await um.AddToRoleAsync(admin, "System Administrator");
        }

        // 4. Assign ALL permissions at Organization level to System Administrator
        var adminRole = await rm.FindByNameAsync("System Administrator");
        if (adminRole != null && !await db.RolePermissions.AnyAsync(rp => rp.RoleId == adminRole.Id))
        {
            var allPerms = await db.Permissions.ToListAsync();
            foreach (var p in allPerms)
                db.RolePermissions.Add(new RolePermission
                { RoleId = adminRole.Id, PermissionId = p.Id, AccessLevel = 2 });
            await db.SaveChangesAsync();
        }
    }
}
