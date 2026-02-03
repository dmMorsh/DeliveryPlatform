using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CourierService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Auto_20260203003416 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "couriers",
                table: "Couriers",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "couriers",
                table: "Couriers");
        }
    }
}
