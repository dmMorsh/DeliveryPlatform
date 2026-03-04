using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Auto_20260304204906_InventoryDbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ProcessedCommands_CorrelationId_CommandType",
                schema: "inventory",
                table: "ProcessedCommands",
                columns: new[] { "CorrelationId", "CommandType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProcessedCommands_CorrelationId_CommandType",
                schema: "inventory",
                table: "ProcessedCommands");
        }
    }
}
