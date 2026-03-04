using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CartService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Auto_20260304180712_CartDbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CheckoutId",
                schema: "cart",
                table: "Carts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CheckoutId",
                schema: "cart",
                table: "Carts");
        }
    }
}
