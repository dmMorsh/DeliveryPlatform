using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Auto_20260203152222 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "inventory",
                table: "StockItems",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateTable(
                name: "ProcessedCommands",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommandType = table.Column<string>(type: "text", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedCommands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockReservation",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReleasedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockReservation", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockItems_ProductId",
                schema: "inventory",
                table: "StockItems",
                column: "ProductId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockReservation_OrderId_ProductId",
                schema: "inventory",
                table: "StockReservation",
                columns: new[] { "OrderId", "ProductId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessedCommands",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "StockReservation",
                schema: "inventory");

            migrationBuilder.DropIndex(
                name: "IX_StockItems_ProductId",
                schema: "inventory",
                table: "StockItems");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "inventory",
                table: "StockItems");
        }
    }
}
