using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Auto_20260128123015 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CostCents_Amount",
                schema: "order",
                table: "Orders");

            migrationBuilder.AddColumn<long>(
                name: "CostCents_AmountCents",
                schema: "order",
                table: "Orders",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CostCents_AmountCents",
                schema: "order",
                table: "Orders");

            migrationBuilder.AddColumn<decimal>(
                name: "CostCents_Amount",
                schema: "order",
                table: "Orders",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
