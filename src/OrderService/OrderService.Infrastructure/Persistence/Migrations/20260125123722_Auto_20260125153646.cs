using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Auto_20260125153646 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CostCents_Amount",
                schema: "order",
                table: "Orders",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

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
                name: "CostCents_Amount",
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
