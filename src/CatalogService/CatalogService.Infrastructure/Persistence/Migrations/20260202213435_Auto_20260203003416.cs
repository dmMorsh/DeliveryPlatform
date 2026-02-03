using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CatalogService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Auto_20260203003416 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "catalog",
                table: "Products",
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
                schema: "catalog",
                table: "Products");
        }
    }
}
