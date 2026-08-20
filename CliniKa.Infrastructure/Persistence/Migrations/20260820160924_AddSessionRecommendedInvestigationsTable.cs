using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CliniKa.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionRecommendedInvestigationsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecommendedInvestigations",
                table: "SessionAIReports");

            migrationBuilder.CreateTable(
                name: "SessionRecommendedInvestigations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    Investigation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Order = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionRecommendedInvestigations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionRecommendedInvestigations_ClinicalSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "ClinicalSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_recommended_investigations_session",
                table: "SessionRecommendedInvestigations",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "idx_recommended_investigations_session_order",
                table: "SessionRecommendedInvestigations",
                columns: new[] { "SessionId", "Order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionRecommendedInvestigations");

            migrationBuilder.AddColumn<string>(
                name: "RecommendedInvestigations",
                table: "SessionAIReports",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
