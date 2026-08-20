using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CliniKa.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionDifferentialDiagnosesAndAlignAiReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Analysis",
                table: "SessionAIReports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChiefComplaint",
                table: "SessionAIReports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompleteFindingsFromAllMedicalImages",
                table: "SessionAIReports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HistoryOfPresentIllness",
                table: "SessionAIReports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImagesReceived",
                table: "SessionAIReports",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImmediateManagement",
                table: "SessionAIReports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PossibleDiagnosis",
                table: "SessionAIReports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecommendedInvestigations",
                table: "SessionAIReports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Transcript",
                table: "SessionAIReports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SessionDifferentialDiagnoses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    Diagnosis = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionDifferentialDiagnoses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionDifferentialDiagnoses_ClinicalSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "ClinicalSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_differential_diagnoses_session",
                table: "SessionDifferentialDiagnoses",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "idx_differential_diagnoses_session_rank",
                table: "SessionDifferentialDiagnoses",
                columns: new[] { "SessionId", "Rank" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionDifferentialDiagnoses");

            migrationBuilder.DropColumn(
                name: "Analysis",
                table: "SessionAIReports");

            migrationBuilder.DropColumn(
                name: "ChiefComplaint",
                table: "SessionAIReports");

            migrationBuilder.DropColumn(
                name: "CompleteFindingsFromAllMedicalImages",
                table: "SessionAIReports");

            migrationBuilder.DropColumn(
                name: "HistoryOfPresentIllness",
                table: "SessionAIReports");

            migrationBuilder.DropColumn(
                name: "ImagesReceived",
                table: "SessionAIReports");

            migrationBuilder.DropColumn(
                name: "ImmediateManagement",
                table: "SessionAIReports");

            migrationBuilder.DropColumn(
                name: "PossibleDiagnosis",
                table: "SessionAIReports");

            migrationBuilder.DropColumn(
                name: "RecommendedInvestigations",
                table: "SessionAIReports");

            migrationBuilder.DropColumn(
                name: "Transcript",
                table: "SessionAIReports");
        }
    }
}
