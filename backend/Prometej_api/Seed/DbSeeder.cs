using Prometej_core.Auth;
using Prometej_core.Models.efModels;
using Prometej_core.Services.Implementations;
using Prometej_persistance;

namespace Prometej_api.Seed
{
    // Registration only ever creates Students, so Teacher and Admin accounts come from
    // the "Seed:Users" configuration section. Existing accounts are never modified.
    public static class DbSeeder
    {
        public static void Run(IServiceProvider services, IConfiguration configuration)
        {
            var seedUsers = configuration.GetSection("Seed:Users").Get<List<SeedUser>>() ?? [];
            var db = services.GetRequiredService<DataContext>();

            foreach (var seedUser in seedUsers)
            {
                if (!Roles.All.Contains(seedUser.Role))
                {
                    throw new InvalidOperationException($"Seed user '{seedUser.Email}' has an unknown role '{seedUser.Role}'.");
                }

                var email = UserService.NormalizeEmail(seedUser.Email);
                if (db.Users.Any(u => u.Email == email)) continue;

                db.Users.Add(new User
                {
                    FirstName = seedUser.FirstName,
                    LastName = seedUser.LastName,
                    Email = email,
                    PasswordHash = PasswordHasher.Hash(seedUser.Password),
                    Role = seedUser.Role,
                });
            }

            db.SaveChanges();
        }
    }
}
