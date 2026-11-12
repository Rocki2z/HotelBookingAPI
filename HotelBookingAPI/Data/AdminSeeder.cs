using HotelBookingAPI.Models;
using Microsoft.AspNetCore.Identity;

namespace HotelBookingAPI.Data;

public static class AdminSeeder
{
    public static async Task SeedAdminAsync(
        IServiceProvider services,
        IConfiguration configuration)
    {
        var userManager =
            services.GetRequiredService<UserManager<ApplicationUser>>();

        var email = configuration["AdminSeed:Email"];
        var password = configuration["AdminSeed:Password"];

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Admin seed email or password is missing.");
        }

        // Check whether this account already exists.
        var admin = await userManager.FindByEmailAsync(email);

        if (admin == null)
        {
            admin = new ApplicationUser
            {
                FullName = "System Administrator",
                Email = email,
                UserName = email,
                EmailConfirmed = true
            };

            var createResult =
                await userManager.CreateAsync(admin, password);

            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join(", ",
                        createResult.Errors.Select(e => e.Description)));
            }
        }

        // Assign Admin role.
        if (!await userManager.IsInRoleAsync(admin, "Admin"))
        {
            var roleResult =
                await userManager.AddToRoleAsync(admin, "Admin");

            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Could not assign Admin role.");
            }
        }
    }
}