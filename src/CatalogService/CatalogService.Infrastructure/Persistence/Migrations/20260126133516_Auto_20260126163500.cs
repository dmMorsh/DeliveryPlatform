using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CatalogService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Auto_20260126163500 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Weight_Value",
                schema: "catalog",
                table: "Products",
                newName: "WeightGrams_Value");

            migrationBuilder.RenameColumn(
                name: "Price_Currency",
                schema: "catalog",
                table: "Products",
                newName: "PriceCents_Currency");

            migrationBuilder.RenameColumn(
                name: "Price_Amount",
                schema: "catalog",
                table: "Products",
                newName: "PriceCents_Amount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WeightGrams_Value",
                schema: "catalog",
                table: "Products",
                newName: "Weight_Value");

            migrationBuilder.RenameColumn(
                name: "PriceCents_Currency",
                schema: "catalog",
                table: "Products",
                newName: "Price_Currency");

            migrationBuilder.RenameColumn(
                name: "PriceCents_Amount",
                schema: "catalog",
                table: "Products",
                newName: "Price_Amount");
        }
    }
}
