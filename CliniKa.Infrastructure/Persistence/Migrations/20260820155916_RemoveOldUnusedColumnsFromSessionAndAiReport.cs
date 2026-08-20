using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CliniKa.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOldUnusedColumnsFromSessionAndAiReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClinicalAlerts",
                table: "SessionAIReports");

            migrationBuilder.DropColumn(
                name: "Contraindications",
                table: "SessionAIReports");

            migrationBuilder.DropColumn(
                name: "DrugInteractions",
                table: "SessionAIReports");

            migrationBuilder.DropColumn(
                name: "PatientSummary",
                table: "SessionAIReports");

            migrationBuilder.DropColumn(
                name: "PossibleDiagnoses",
                table: "SessionAIReports");

            migrationBuilder.DropColumn(
                name: "SuggestedInvestigations",
                table: "SessionAIReports");

            migrationBuilder.DropColumn(
                name: "AudioTranscript",
                table: "ClinicalSessions");

            migrationBuilder.DropColumn(
                name: "ExtractedSymptoms",
                table: "ClinicalSessions");

            migrationBuilder.DropColumn(
                name: "MentionedHistory",
                table: "ClinicalSessions");

            migrationBuilder.DropColumn(
                name: "MentionedMedications",
                table: "ClinicalSessions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClinicalAlerts",
                table: "SessionAIReports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Contraindications",
                table: "SessionAIReports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DrugInteractions",
                table: "SessionAIReports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PatientSummary",
                table: "SessionAIReports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PossibleDiagnoses",
                table: "SessionAIReports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuggestedInvestigations",
                table: "SessionAIReports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AudioTranscript",
                table: "ClinicalSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExtractedSymptoms",
                table: "ClinicalSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MentionedHistory",
                table: "ClinicalSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MentionedMedications",
                table: "ClinicalSessions",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
