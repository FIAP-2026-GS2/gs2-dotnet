using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SampaTech.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReferenceTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ISSUE_TYPE",
                columns: table => new
                {
                    ID_ISSUE_TYPE = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NAME = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CATEGORY = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DESCRIPTION = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IS_ACTIVE = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ISSUE_TYPE", x => x.ID_ISSUE_TYPE);
                });

            migrationBuilder.CreateTable(
                name: "MUNICIPAL_AGENCY",
                columns: table => new
                {
                    ID_AGENCY = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NAME = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DESCRIPTION = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CITY = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    STATE = table.Column<string>(type: "nchar(2)", fixedLength: true, maxLength: 2, nullable: false),
                    IS_ACTIVE = table.Column<bool>(type: "bit", nullable: false),
                    SUBPREFECTURE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MUNICIPAL_AGENCY", x => x.ID_AGENCY);
                });

            migrationBuilder.CreateTable(
                name: "TRIGGER_EVENT",
                columns: table => new
                {
                    ID_EVENT = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EVENT_TYPE = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DESCRIPTION = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    INTENSITY = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ALERT_SOURCE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    START_DATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    END_DATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    STATUS = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRIGGER_EVENT", x => x.ID_EVENT);
                });

            migrationBuilder.CreateTable(
                name: "TEAM",
                columns: table => new
                {
                    ID_TEAM = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NAME = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DESCRIPTION = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IS_ACTIVE = table.Column<bool>(type: "bit", nullable: false),
                    ID_AGENCY = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TEAM", x => x.ID_TEAM);
                    table.ForeignKey(
                        name: "FK_TEAM_MUNICIPAL_AGENCY_ID_AGENCY",
                        column: x => x.ID_AGENCY,
                        principalTable: "MUNICIPAL_AGENCY",
                        principalColumn: "ID_AGENCY",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ISSUE_TYPE_NAME",
                table: "ISSUE_TYPE",
                column: "NAME",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TEAM_ID_AGENCY",
                table: "TEAM",
                column: "ID_AGENCY");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ISSUE_TYPE");

            migrationBuilder.DropTable(
                name: "TEAM");

            migrationBuilder.DropTable(
                name: "TRIGGER_EVENT");

            migrationBuilder.DropTable(
                name: "MUNICIPAL_AGENCY");
        }
    }
}
