using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CatalogService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Auto_20260128123015 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriceCents_Amount",
                schema: "catalog",
                table: "Products");

            migrationBuilder.AlterColumn<long>(
                name: "WeightGrams_Value",
                schema: "catalog",
                table: "Products",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<long>(
                name: "PriceCents_AmountCents",
                schema: "catalog",
                table: "Products",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriceCents_AmountCents",
                schema: "catalog",
                table: "Products");

            migrationBuilder.AlterColumn<decimal>(
                name: "WeightGrams_Value",
                schema: "catalog",
                table: "Products",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<decimal>(
                name: "PriceCents_Amount",
                schema: "catalog",
                table: "Products",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
