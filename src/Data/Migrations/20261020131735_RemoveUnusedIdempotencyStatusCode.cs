using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace payment_gateway_API.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnusedIdempotencyStatusCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StatusCode",
                table: "IdempotencyRecords");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StatusCode",
                table: "IdempotencyRecords",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
