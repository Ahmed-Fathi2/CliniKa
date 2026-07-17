using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CliniKa.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedDoctorAndPatient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DoctorId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PatientId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b74ddd14-6340-4840-95c2-db12554843e5",
                columns: new[] { "DoctorId", "PasswordHash", "PatientId" },
                values: new object[] { null, "AQAAAAIAAYagAAAAENG89Ckz0JyxHoDFcsHCfu2gHIZenH3nbu7HvWo4TADxbT94S7/KjFXJmmwqZU+vfQ==", null });

            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "ConcurrencyStamp", "DoctorId", "Email", "EmailConfirmed", "FirstName", "LastName", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PatientId", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "TwoFactorEnabled", "UserName" },
                values: new object[,]
                {
                    { "d1b2c3d4-e5f6-7890-abcd-ef1234567890", 0, "8c6a9b4d-7c2a-4c9b-9a7e-8c3b6f2e1a9d", null, "doctor@clinika.com", true, "Ahmad", "Fathi", false, null, "DOCTOR@CLINIKA.COM", "DOCTOR@CLINIKA.COM", "AQAAAAIAAYagAAAAENG89Ckz0JyxHoDFcsHCfu2gHIZenH3nbu7HvWo4TADxbT94S7/KjFXJmmwqZU+vfQ==", null, null, false, "7c6a9b4d-7c2a-4c9b-9a7e-8c3b6f2e1a9d", false, "doctor@clinika.com" },
                    { "p1b2c3d4-e5f6-7890-abcd-ef1234567890", 0, "6c6a9b4d-7c2a-4c9b-9a7e-8c3b6f2e1a9d", null, "patient@clinika.com", true, "John", "Doe", false, null, "PATIENT@CLINIKA.COM", "PATIENT@CLINIKA.COM", "AQAAAAIAAYagAAAAENG89Ckz0JyxHoDFcsHCfu2gHIZenH3nbu7HvWo4TADxbT94S7/KjFXJmmwqZU+vfQ==", null, null, false, "5c6a9b4d-7c2a-4c9b-9a7e-8c3b6f2e1a9d", false, "patient@clinika.com" }
                });

            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[,]
                {
                    { "2c5e174e-3b0e-446f-86af-483d56fd7210", "d1b2c3d4-e5f6-7890-abcd-ef1234567890" },
                    { "a1b2c3d4-e5f6-7890-abcd-ef1234567890", "p1b2c3d4-e5f6-7890-abcd-ef1234567890" }
                });

            migrationBuilder.InsertData(
                table: "Doctors",
                columns: new[] { "Id", "CreatedAt", "FullName", "LicenseNumber", "Specialty", "UserId" },
                values: new object[] { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Dr. Ahmad Fathi", "DOC12345", "Cardiology", "d1b2c3d4-e5f6-7890-abcd-ef1234567890" });

            migrationBuilder.InsertData(
                table: "Patients",
                columns: new[] { "Id", "BloodType", "CreatedAt", "DateOfBirth", "FullName", "Gender", "NationalId", "Phone", "UserId" },
                values: new object[] { 1, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(1990, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "John Doe", "Male", null, null, "p1b2c3d4-e5f6-7890-abcd-ef1234567890" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_DoctorId",
                table: "AspNetUsers",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_PatientId",
                table: "AspNetUsers",
                column: "PatientId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Doctors_DoctorId",
                table: "AspNetUsers",
                column: "DoctorId",
                principalTable: "Doctors",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Patients_PatientId",
                table: "AspNetUsers",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Doctors_DoctorId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Patients_PatientId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_DoctorId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_PatientId",
                table: "AspNetUsers");

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "2c5e174e-3b0e-446f-86af-483d56fd7210", "d1b2c3d4-e5f6-7890-abcd-ef1234567890" });

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "a1b2c3d4-e5f6-7890-abcd-ef1234567890", "p1b2c3d4-e5f6-7890-abcd-ef1234567890" });

            migrationBuilder.DeleteData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Patients",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "d1b2c3d4-e5f6-7890-abcd-ef1234567890");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "p1b2c3d4-e5f6-7890-abcd-ef1234567890");

            migrationBuilder.DropColumn(
                name: "DoctorId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PatientId",
                table: "AspNetUsers");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b74ddd14-6340-4840-95c2-db12554843e5",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEJ0z5M8i3f4+v6K/V+v6+v6+v6+v6+v6+v6+v6+v6+v6+v6+v6+v6+v6+v6+v6+v6w==");
        }
    }
}
