using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CliniKa.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AllowCustomPatientDiseasesAndAllergies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_PatientDiseases",
                table: "PatientDiseases");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PatientAllergies",
                table: "PatientAllergies");

            migrationBuilder.DropIndex(
                name: "idx_active_meds",
                table: "ActiveMedications");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ActiveMedications");

            migrationBuilder.AlterColumn<int>(
                name: "DiseaseId",
                table: "PatientDiseases",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "PatientDiseases",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<string>(
                name: "RawDiseaseName",
                table: "PatientDiseases",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AllergyId",
                table: "PatientAllergies",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "PatientAllergies",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<string>(
                name: "RawAllergenName",
                table: "PatientAllergies",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PatientDiseases",
                table: "PatientDiseases",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PatientAllergies",
                table: "PatientAllergies",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "ChatSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, defaultValue: "New Conversation"),
                    SystemPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MessagesJson = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "[]"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    LastMessageAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatSessions_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_active_meds",
                table: "ActiveMedications",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "idx_chatsessions_patient_lastmsg",
                table: "ChatSessions",
                columns: new[] { "PatientId", "LastMessageAt" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatSessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PatientDiseases",
                table: "PatientDiseases");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PatientAllergies",
                table: "PatientAllergies");

            migrationBuilder.DropIndex(
                name: "idx_active_meds",
                table: "ActiveMedications");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "PatientDiseases");

            migrationBuilder.DropColumn(
                name: "RawDiseaseName",
                table: "PatientDiseases");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "PatientAllergies");

            migrationBuilder.DropColumn(
                name: "RawAllergenName",
                table: "PatientAllergies");

            migrationBuilder.AlterColumn<int>(
                name: "DiseaseId",
                table: "PatientDiseases",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AllergyId",
                table: "PatientAllergies",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ActiveMedications",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PatientDiseases",
                table: "PatientDiseases",
                columns: new[] { "PatientId", "DiseaseId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_PatientAllergies",
                table: "PatientAllergies",
                columns: new[] { "PatientId", "AllergyId" });

            migrationBuilder.CreateIndex(
                name: "idx_active_meds",
                table: "ActiveMedications",
                column: "PatientId",
                filter: "[IsActive] = 1");
        }
    }
}
