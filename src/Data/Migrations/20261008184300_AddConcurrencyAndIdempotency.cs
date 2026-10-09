using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace payment_gateway_API.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddConcurrencyAndIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Contas_UsuarioId",
                table: "Contas");

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Contas",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "CodigosPagamento",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.CreateTable(
                name: "IdempotencyRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Operation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResponseJson = table.Column<string>(type: "text", nullable: false),
                    StatusCode = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Contas_UsuarioId",
                table: "Contas",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecords_UserId_Operation_Key",
                table: "IdempotencyRecords",
                columns: new[] { "UserId", "Operation", "Key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdempotencyRecords");

            migrationBuilder.DropIndex(
                name: "IX_Contas_UsuarioId",
                table: "Contas");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Contas");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "CodigosPagamento");

            migrationBuilder.CreateIndex(
                name: "IX_Contas_UsuarioId",
                table: "Contas",
                column: "UsuarioId");
        }
    }
}
