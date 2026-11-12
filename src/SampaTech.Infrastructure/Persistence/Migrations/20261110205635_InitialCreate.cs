using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SampaTech.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MONITORING_POINT",
                columns: table => new
                {
                    ID_POINT = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    STREET = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    STREET_NUMBER = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    NEIGHBORHOOD = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    CITY = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    STATE = table.Column<string>(type: "nchar(2)", fixedLength: true, maxLength: 2, nullable: false),
                    LATITUDE = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    LONGITUDE = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    DESCRIPTION = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MONITORING_POINT", x => x.ID_POINT);
                });

            migrationBuilder.CreateTable(
                name: "CAMERA",
                columns: table => new
                {
                    ID_CAMERA = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CAMERA_CODE = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DESCRIPTION = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    STATUS = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    INSTALLATION_DATE = table.Column<DateOnly>(type: "date", nullable: true),
                    ID_POINT = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CAMERA", x => x.ID_CAMERA);
                    table.ForeignKey(
                        name: "FK_CAMERA_MONITORING_POINT_ID_POINT",
                        column: x => x.ID_POINT,
                        principalTable: "MONITORING_POINT",
                        principalColumn: "ID_POINT",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CAMERA_CAMERA_CODE",
                table: "CAMERA",
                column: "CAMERA_CODE",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CAMERA_ID_POINT",
                table: "CAMERA",
                column: "ID_POINT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CAMERA");

            migrationBuilder.DropTable(
                name: "MONITORING_POINT");
        }
    }
}
