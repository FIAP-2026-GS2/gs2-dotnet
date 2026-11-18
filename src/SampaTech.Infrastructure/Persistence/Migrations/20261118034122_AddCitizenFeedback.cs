using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SampaTech.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCitizenFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CITIZEN_FEEDBACK",
                columns: table => new
                {
                    ID_FEEDBACK = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ID_ISSUE_REPORT = table.Column<int>(type: "int", nullable: false),
                    ID_CAPTURE = table.Column<int>(type: "int", nullable: false),
                    ID_USER = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DECISION = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    STATUS = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    NOTES = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "datetime2", nullable: false),
                    REVIEWED_AT = table.Column<DateTime>(type: "datetime2", nullable: true),
                    REVIEWED_BY = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    REVIEW_NOTES = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CITIZEN_FEEDBACK", x => x.ID_FEEDBACK);
                    table.ForeignKey(
                        name: "FK_CITIZEN_FEEDBACK_CAPTURE_ID_CAPTURE",
                        column: x => x.ID_CAPTURE,
                        principalTable: "CAPTURE",
                        principalColumn: "ID_CAPTURE",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CITIZEN_FEEDBACK_ISSUE_REPORT_ID_ISSUE_REPORT",
                        column: x => x.ID_ISSUE_REPORT,
                        principalTable: "ISSUE_REPORT",
                        principalColumn: "ID_ISSUE_REPORT",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CITIZEN_FEEDBACK_ID_CAPTURE",
                table: "CITIZEN_FEEDBACK",
                column: "ID_CAPTURE");

            migrationBuilder.CreateIndex(
                name: "IX_CITIZEN_FEEDBACK_ISSUE_STATUS",
                table: "CITIZEN_FEEDBACK",
                columns: new[] { "ID_ISSUE_REPORT", "STATUS" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CITIZEN_FEEDBACK");
        }
    }
}
