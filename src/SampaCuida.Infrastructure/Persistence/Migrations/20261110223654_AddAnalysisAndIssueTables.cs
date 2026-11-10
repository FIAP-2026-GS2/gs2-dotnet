using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SampaCuida.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalysisAndIssueTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AI_ANALYSIS",
                columns: table => new
                {
                    ID_ANALYSIS = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ID_IMAGE = table.Column<int>(type: "int", nullable: false),
                    AI_MODEL = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MODEL_VERSION = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    STATUS = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ANALYSIS_DATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PROCESSING_TIME_MS = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AI_ANALYSIS", x => x.ID_ANALYSIS);
                    table.ForeignKey(
                        name: "FK_AI_ANALYSIS_IMAGE_ID_IMAGE",
                        column: x => x.ID_IMAGE,
                        principalTable: "IMAGE",
                        principalColumn: "ID_IMAGE",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ISSUE_REPORT",
                columns: table => new
                {
                    ID_ISSUE_REPORT = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TITLE = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DESCRIPTION = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SOURCE = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SEVERITY = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    STATUS = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OPENING_DATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RESOLUTION_DATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LEVEL = table.Column<int>(type: "int", nullable: true),
                    SIDE = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ID_ISSUE_TYPE = table.Column<int>(type: "int", nullable: false),
                    ID_USER = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ISSUE_REPORT", x => x.ID_ISSUE_REPORT);
                    table.ForeignKey(
                        name: "FK_ISSUE_REPORT_ISSUE_TYPE_ID_ISSUE_TYPE",
                        column: x => x.ID_ISSUE_TYPE,
                        principalTable: "ISSUE_TYPE",
                        principalColumn: "ID_ISSUE_TYPE",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AI_DETECTION",
                columns: table => new
                {
                    ID_DETECTION = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ID_ANALYSIS = table.Column<int>(type: "int", nullable: false),
                    ID_ISSUE_TYPE = table.Column<int>(type: "int", nullable: false),
                    CONFIDENCE_SCORE = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: true),
                    ESTIMATED_SEVERITY = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DETECTION_COORDINATES = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DESCRIPTION = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ID_ISSUE_REPORT = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AI_DETECTION", x => x.ID_DETECTION);
                    table.ForeignKey(
                        name: "FK_AI_DETECTION_AI_ANALYSIS_ID_ANALYSIS",
                        column: x => x.ID_ANALYSIS,
                        principalTable: "AI_ANALYSIS",
                        principalColumn: "ID_ANALYSIS",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AI_DETECTION_ISSUE_REPORT_ID_ISSUE_REPORT",
                        column: x => x.ID_ISSUE_REPORT,
                        principalTable: "ISSUE_REPORT",
                        principalColumn: "ID_ISSUE_REPORT",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AI_DETECTION_ISSUE_TYPE_ID_ISSUE_TYPE",
                        column: x => x.ID_ISSUE_TYPE,
                        principalTable: "ISSUE_TYPE",
                        principalColumn: "ID_ISSUE_TYPE",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ISSUE_ASSIGNMENT",
                columns: table => new
                {
                    ID_ASSIGNMENT = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ID_ISSUE_REPORT = table.Column<int>(type: "int", nullable: false),
                    ID_TEAM = table.Column<int>(type: "int", nullable: false),
                    ASSIGNMENT_DATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CLOSING_DATE = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ISSUE_ASSIGNMENT", x => x.ID_ASSIGNMENT);
                    table.ForeignKey(
                        name: "FK_ISSUE_ASSIGNMENT_ISSUE_REPORT_ID_ISSUE_REPORT",
                        column: x => x.ID_ISSUE_REPORT,
                        principalTable: "ISSUE_REPORT",
                        principalColumn: "ID_ISSUE_REPORT",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ISSUE_ASSIGNMENT_TEAM_ID_TEAM",
                        column: x => x.ID_TEAM,
                        principalTable: "TEAM",
                        principalColumn: "ID_TEAM",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ISSUE_HISTORY",
                columns: table => new
                {
                    ID_HISTORY = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ID_ISSUE_REPORT = table.Column<int>(type: "int", nullable: false),
                    PREVIOUS_STATUS = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    NEW_STATUS = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NOTES = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CHANGE_DATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ID_USER = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ISSUE_HISTORY", x => x.ID_HISTORY);
                    table.ForeignKey(
                        name: "FK_ISSUE_HISTORY_ISSUE_REPORT_ID_ISSUE_REPORT",
                        column: x => x.ID_ISSUE_REPORT,
                        principalTable: "ISSUE_REPORT",
                        principalColumn: "ID_ISSUE_REPORT",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AI_ANALYSIS_ID_IMAGE",
                table: "AI_ANALYSIS",
                column: "ID_IMAGE");

            migrationBuilder.CreateIndex(
                name: "IX_AI_DETECTION_ID_ANALYSIS",
                table: "AI_DETECTION",
                column: "ID_ANALYSIS");

            migrationBuilder.CreateIndex(
                name: "IX_AI_DETECTION_ID_ISSUE_REPORT",
                table: "AI_DETECTION",
                column: "ID_ISSUE_REPORT");

            migrationBuilder.CreateIndex(
                name: "IX_AI_DETECTION_ID_ISSUE_TYPE",
                table: "AI_DETECTION",
                column: "ID_ISSUE_TYPE");

            migrationBuilder.CreateIndex(
                name: "IX_ISSUE_ASSIGNMENT_ID_TEAM",
                table: "ISSUE_ASSIGNMENT",
                column: "ID_TEAM");

            migrationBuilder.CreateIndex(
                name: "UX_ISSUE_ASSIGNMENT_OPEN",
                table: "ISSUE_ASSIGNMENT",
                column: "ID_ISSUE_REPORT",
                unique: true,
                filter: "[CLOSING_DATE] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ISSUE_HISTORY_ID_ISSUE_REPORT",
                table: "ISSUE_HISTORY",
                column: "ID_ISSUE_REPORT");

            migrationBuilder.CreateIndex(
                name: "IX_ISSUE_REPORT_ID_ISSUE_TYPE",
                table: "ISSUE_REPORT",
                column: "ID_ISSUE_TYPE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AI_DETECTION");

            migrationBuilder.DropTable(
                name: "ISSUE_ASSIGNMENT");

            migrationBuilder.DropTable(
                name: "ISSUE_HISTORY");

            migrationBuilder.DropTable(
                name: "AI_ANALYSIS");

            migrationBuilder.DropTable(
                name: "ISSUE_REPORT");
        }
    }
}
