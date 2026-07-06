using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CliniKa.Domain.Entities;
using CliniKa.Infrastructure.Persistence.Identity;

namespace CliniKa.Infrastructure.Persistence.Seeders
{
    public static class RoleAndUserSeeder
    {
        public static void SeedRolesAndUser(this ModelBuilder builder)
        {
            const string adminRoleId = "8e445865-a24d-4543-a6c6-9443d048cdb9";
            const string doctorRoleId = "2c5e174e-3b0e-446f-86af-483d56fd7210";
            const string patientRoleId = "a1b2c3d4-e5f6-7890-abcd-ef1234567890";
            const string adminUserId = "b74ddd14-6340-4840-95c2-db12554843e5";
            // Pass = Admin123!

            builder.Entity<ApplicationRole>().HasData(
                new ApplicationRole
                {
                    Id = adminRoleId,
                    Name = "Admin",
                    NormalizedName = "ADMIN",
                    ConcurrencyStamp = "1",
                    RoleDescription = "System Administrator"
                },
                new ApplicationRole
                {
                    Id = doctorRoleId,
                    Name = "Doctor",
                    NormalizedName = "DOCTOR",
                    ConcurrencyStamp = "2",
                    RoleDescription = "Medical Doctor"
                },
                new ApplicationRole
                {
                    Id = patientRoleId,
                    Name = "Patient",
                    NormalizedName = "PATIENT",
                    ConcurrencyStamp = "3",
                    RoleDescription = "Patient User"
                }
            );

            // Seed Admin User
            var adminUser = new ApplicationUser
            {
                Id = adminUserId,
                UserName = "admin@clinika.com",
                NormalizedUserName = "ADMIN@CLINIKA.COM",
                Email = "admin@clinika.com",
                NormalizedEmail = "ADMIN@CLINIKA.COM",
                EmailConfirmed = true,
                FirstName = "Admin",
                LastName = "CliniKa",
                SecurityStamp = "d87a4b0d-7c2a-4c9b-9a7e-8c3b6f2e1a9d",
                ConcurrencyStamp = "8e445865-a24d-4543-a6c6-9443d048cdb9",
                PasswordHash = "AQAAAAIAAYagAAAAEJ0z5M8i3f4+v6K/V+v6+v6+v6+v6+v6+v6+v6+v6+v6+v6+v6+v6+v6+v6+v6+v6w=="
            };

            builder.Entity<ApplicationUser>().HasData(adminUser);

            builder.Entity<IdentityUserRole<string>>().HasData(new IdentityUserRole<string>
            {
                RoleId = adminRoleId,
                UserId = adminUserId
            });
        }
    }
}
