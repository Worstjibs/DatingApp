using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using API.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace API.Data {
    public class Seed {
        public static async Task SeedUsers(
            UserManager<AppUser> userManager, 
            RoleManager<AppRole> roleManager
        ) {
            // Check if any users exist before seeding
            if (await userManager.Users.AnyAsync()) return;

            // Read the JSON file containing User seed data
            var userData = await System.IO.File.ReadAllTextAsync("Data/UserSeedData.json");
            var users = JsonSerializer.Deserialize<List<AppUser>>(userData);
            if (users ==  null) return;

            // Create some roles
            var roles = new List<AppRole> {
                new AppRole { Name = "Member" },
                new AppRole { Name = "Admin" },
                new AppRole { Name = "Moderator" }
            };

            foreach (var role in roles) {
                await roleManager.CreateAsync(role);
            }

            // Add users to DB and to the Member role
            foreach (var user in users) {
                user.UserName = user.UserName.ToLower();
                await userManager.CreateAsync(user, "Passwerd");
                await userManager.AddToRoleAsync(user, "Member");
            }

            // Add an Admin User and add it to Admin and Moderator roles
            var admin = new AppUser {
                UserName = "admin"
            };

            await userManager.CreateAsync(admin, "Passwerd");
            await userManager.AddToRolesAsync(admin, new[] { "Admin", "Moderator" });
        }
    }
}