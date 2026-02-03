using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Auto_20260203003416 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "order",
                table: "Orders",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                schema: "order",
                table: "OrderItem",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "order",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "order",
                table: "OrderItem");
        }
    }
}
