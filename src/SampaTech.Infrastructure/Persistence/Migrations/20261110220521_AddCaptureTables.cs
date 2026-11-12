using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SampaTech.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCaptureTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CAPTURE_SCHEDULE",
                columns: table => new
                {
                    ID_SCHEDULE = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ID_CAMERA = table.Column<int>(type: "int", nullable: false),
                    WEEKDAY = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    EXECUTION_TIME = table.Column<TimeOnly>(type: "time", nullable: false),
                    START_DATE = table.Column<DateOnly>(type: "date", nullable: false),
                    END_DATE = table.Column<DateOnly>(type: "date", nullable: true),
                    IS_ACTIVE = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CAPTURE_SCHEDULE", x => x.ID_SCHEDULE);
                    table.ForeignKey(
                        name: "FK_CAPTURE_SCHEDULE_CAMERA_ID_CAMERA",
                        column: x => x.ID_CAMERA,
                        principalTable: "CAMERA",
                        principalColumn: "ID_CAMERA",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CAPTURE",
                columns: table => new
                {
                    ID_CAPTURE = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SOURCE = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CAPTURE_DATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    STATUS = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LATITUDE = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    LONGITUDE = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    ID_CAMERA = table.Column<int>(type: "int", nullable: true),
                    ID_SCHEDULE = table.Column<int>(type: "int", nullable: true),
                    ID_EVENT = table.Column<int>(type: "int", nullable: true),
                    ID_USER = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CAPTURE", x => x.ID_CAPTURE);
                    table.ForeignKey(
                        name: "FK_CAPTURE_CAMERA_ID_CAMERA",
                        column: x => x.ID_CAMERA,
                        principalTable: "CAMERA",
                        principalColumn: "ID_CAMERA",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CAPTURE_CAPTURE_SCHEDULE_ID_SCHEDULE",
                        column: x => x.ID_SCHEDULE,
                        principalTable: "CAPTURE_SCHEDULE",
                        principalColumn: "ID_SCHEDULE",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CAPTURE_TRIGGER_EVENT_ID_EVENT",
                        column: x => x.ID_EVENT,
                        principalTable: "TRIGGER_EVENT",
                        principalColumn: "ID_EVENT",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IMAGE",
                columns: table => new
                {
                    ID_IMAGE = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ID_CAPTURE = table.Column<int>(type: "int", nullable: false),
                    FILE_NAME = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FILE_PATH = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FILE_HASH = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: true),
                    STORAGE_DATE = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IMAGE", x => x.ID_IMAGE);
                    table.ForeignKey(
                        name: "FK_IMAGE_CAPTURE_ID_CAPTURE",
                        column: x => x.ID_CAPTURE,
                        principalTable: "CAPTURE",
                        principalColumn: "ID_CAPTURE",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CAPTURE_ID_EVENT",
                table: "CAPTURE",
                column: "ID_EVENT");

            migrationBuilder.CreateIndex(
                name: "IX_CAPTURE_ID_SCHEDULE",
                table: "CAPTURE",
                column: "ID_SCHEDULE");

            migrationBuilder.CreateIndex(
                name: "UX_CAPTURE_CAMERA_DATE",
                table: "CAPTURE",
                columns: new[] { "ID_CAMERA", "CAPTURE_DATE" },
                unique: true,
                filter: "[ID_CAMERA] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CAPTURE_SCHEDULE_ID_CAMERA",
                table: "CAPTURE_SCHEDULE",
                column: "ID_CAMERA");

            migrationBuilder.CreateIndex(
                name: "IX_IMAGE_ID_CAPTURE",
                table: "IMAGE",
                column: "ID_CAPTURE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IMAGE");

            migrationBuilder.DropTable(
                name: "CAPTURE");

            migrationBuilder.DropTable(
                name: "CAPTURE_SCHEDULE");
        }
    }
}
