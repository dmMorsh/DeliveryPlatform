using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Auto_20260313222101_KitchenDbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "kitchen");

            migrationBuilder.RenameTable(
                name: "KitchenSlots",
                schema: "Kitchen",
                newName: "KitchenSlots",
                newSchema: "kitchen");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Kitchen");

            migrationBuilder.RenameTable(
                name: "KitchenSlots",
                schema: "kitchen",
                newName: "KitchenSlots",
                newSchema: "Kitchen");
        }
    }
}
