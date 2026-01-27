using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Auto_20260126163500 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Price",
                schema: "order",
                table: "OrderItem",
                newName: "PriceCents");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PriceCents",
                schema: "order",
                table: "OrderItem",
                newName: "Price");
        }
    }
}
