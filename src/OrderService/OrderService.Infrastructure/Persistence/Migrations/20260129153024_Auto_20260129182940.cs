using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Auto_20260129182940 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CostCents_AmountCents",
                schema: "order",
                table: "Orders",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "CostCents_Currency",
                schema: "order",
                table: "Orders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "From_Latitude",
                schema: "order",
                table: "Orders",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "From_Longitude",
                schema: "order",
                table: "Orders",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "From_Street",
                schema: "order",
                table: "Orders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "To_Latitude",
                schema: "order",
                table: "Orders",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "To_Longitude",
                schema: "order",
                table: "Orders",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "To_Street",
                schema: "order",
                table: "Orders",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CostCents_AmountCents",
                schema: "order",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CostCents_Currency",
                schema: "order",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "From_Latitude",
                schema: "order",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "From_Longitude",
                schema: "order",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "From_Street",
                schema: "order",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "To_Latitude",
                schema: "order",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "To_Longitude",
                schema: "order",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "To_Street",
                schema: "order",
                table: "Orders");
        }
    }
}
