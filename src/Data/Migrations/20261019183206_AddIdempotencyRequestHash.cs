using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace payment_gateway_API.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIdempotencyRequestHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RequestHash",
                table: "IdempotencyRecords",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequestHash",
                table: "IdempotencyRecords");
        }
    }
}
