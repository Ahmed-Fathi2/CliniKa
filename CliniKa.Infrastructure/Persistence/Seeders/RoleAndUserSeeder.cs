using CliniKa.Domain.Entities;
using CliniKa.Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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
            const string doctorUserId = "d1b2c3d4-e5f6-7890-abcd-ef1234567890";
            const string patientUserId = "p1b2c3d4-e5f6-7890-abcd-ef1234567890";

            // Roles
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

            // Admin
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
                IsActive = true,
                SecurityStamp = "d87a4b0d-7c2a-4c9b-9a7e-8c3b6f2e1a9d",
                ConcurrencyStamp = "8e445865-a24d-4543-a6c6-9443d048cdb9",
                PasswordHash = "AQAAAAIAAYagAAAAENG89Ckz0JyxHoDFcsHCfu2gHIZenH3nbu7HvWo4TADxbT94S7/KjFXJmmwqZU+vfQ=="
            };

            // Doctor User
            var doctorUser = new ApplicationUser
            {
                Id = doctorUserId,
                UserName = "doctor@clinika.com",
                NormalizedUserName = "DOCTOR@CLINIKA.COM",
                Email = "doctor@clinika.com",
                NormalizedEmail = "DOCTOR@CLINIKA.COM",
                EmailConfirmed = true,
                FirstName = "Ahmad",
                LastName = "Fathi",
                IsActive = true,
                SecurityStamp = "7c6a9b4d-7c2a-4c9b-9a7e-8c3b6f2e1a9d",
                ConcurrencyStamp = "8c6a9b4d-7c2a-4c9b-9a7e-8c3b6f2e1a9d",
                PasswordHash = "AQAAAAIAAYagAAAAENG89Ckz0JyxHoDFcsHCfu2gHIZenH3nbu7HvWo4TADxbT94S7/KjFXJmmwqZU+vfQ=="
            };

            // Patient User
            var patientUser = new ApplicationUser
            {
                Id = patientUserId,
                UserName = "patient@clinika.com",
                NormalizedUserName = "PATIENT@CLINIKA.COM",
                Email = "patient@clinika.com",
                NormalizedEmail = "PATIENT@CLINIKA.COM",
                EmailConfirmed = true,
                FirstName = "John",
                LastName = "Doe",
                IsActive = true,
                SecurityStamp = "5c6a9b4d-7c2a-4c9b-9a7e-8c3b6f2e1a9d",
                ConcurrencyStamp = "6c6a9b4d-7c2a-4c9b-9a7e-8c3b6f2e1a9d",
                PasswordHash = "AQAAAAIAAYagAAAAENG89Ckz0JyxHoDFcsHCfu2gHIZenH3nbu7HvWo4TADxbT94S7/KjFXJmmwqZU+vfQ=="
            };

            builder.Entity<ApplicationUser>().HasData(adminUser, doctorUser, patientUser);

            // User-Roles
            builder.Entity<IdentityUserRole<string>>().HasData(
                new IdentityUserRole<string>
                {
                    RoleId = adminRoleId,
                    UserId = adminUserId
                },
                new IdentityUserRole<string>
                {
                    RoleId = doctorRoleId,
                    UserId = doctorUserId
                },
                new IdentityUserRole<string>
                {
                    RoleId = patientRoleId,
                    UserId = patientUserId
                }
            );

            // Seed Doctor Entity
            builder.Entity<Doctor>().HasData(new Doctor
            {
                Id = 1,
                FullName = "Dr. Ahmad Fathi",
                UserId = doctorUserId,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });

            // Seed Patient Entity
            builder.Entity<Patient>().HasData(new Patient
            {
                Id = 1,
                FullName = "John Doe",
                DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Gender = "Male",
                UserId = patientUserId,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
        }
    }
}
