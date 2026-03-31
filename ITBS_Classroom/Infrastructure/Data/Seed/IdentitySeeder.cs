using ITBS_Classroom.Models;
using Microsoft.AspNetCore.Identity;

namespace ITBS_Classroom.Infrastructure.Data.Seed;

public static class IdentitySeeder
{
    public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        string[] roles = [ApplicationRoles.Admin, ApplicationRoles.Teacher, ApplicationRoles.Student];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    public static async Task SeedAdminAsync(UserManager<ApplicationUser> userManager, string email, string password)
    {
        var existingAdmin = await userManager.FindByEmailAsync(email);
        if (existingAdmin is not null)
        {
            if (!await userManager.IsInRoleAsync(existingAdmin, ApplicationRoles.Admin))
            {
                await userManager.AddToRoleAsync(existingAdmin, ApplicationRoles.Admin);
            }

            return;
        }

        var admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Super",
            LastName = "Admin",
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(admin, password);
        if (createResult.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, ApplicationRoles.Admin);
        }
    }
}
