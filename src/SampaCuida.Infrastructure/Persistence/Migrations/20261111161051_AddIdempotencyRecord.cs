using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SampaCuida.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIdempotencyRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IDEMPOTENCY_RECORD",
                columns: table => new
                {
                    IDEMPOTENCY_KEY = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    REQUEST_HASH = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    STATUS_CODE = table.Column<int>(type: "int", nullable: true),
                    RESPONSE_BODY = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IDEMPOTENCY_RECORD", x => x.IDEMPOTENCY_KEY);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IDEMPOTENCY_RECORD");
        }
    }
}
