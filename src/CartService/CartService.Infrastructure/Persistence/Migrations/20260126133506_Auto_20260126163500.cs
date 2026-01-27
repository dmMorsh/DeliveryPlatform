using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CartService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Auto_20260126163500 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Price",
                schema: "cart",
                table: "CartItem",
                newName: "PriceCents");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PriceCents",
                schema: "cart",
                table: "CartItem",
                newName: "Price");
        }
    }
}
