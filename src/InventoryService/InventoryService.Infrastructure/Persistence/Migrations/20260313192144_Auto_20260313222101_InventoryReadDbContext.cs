using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Auto_20260313222101_InventoryReadDbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "inventory_read");

            migrationBuilder.RenameTable(
                name: "StockItems",
                newName: "StockItems",
                newSchema: "inventory_read");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "StockItems",
                schema: "inventory_read",
                newName: "StockItems");
        }
    }
}
