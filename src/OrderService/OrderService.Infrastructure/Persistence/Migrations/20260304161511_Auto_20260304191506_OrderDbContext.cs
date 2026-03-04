using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Auto_20260304191506_OrderDbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AcceptedAt",
                schema: "order",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CourierName",
                schema: "order",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CourierPhone",
                schema: "order",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DeliveryFeeMultiplier",
                schema: "order",
                table: "Orders",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryPickupSlaMinutes",
                schema: "order",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryTransitSlaMinutes",
                schema: "order",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DeliveryZoneDistanceKm",
                schema: "order",
                table: "Orders",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryZoneId",
                schema: "order",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryZoneName",
                schema: "order",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpectedReadyAt",
                schema: "order",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsReadyForDelivery",
                schema: "order",
                table: "Orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "KitchenDelayedNotifiedAt",
                schema: "order",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "KitchenSlotStart",
                schema: "order",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReadyAt",
                schema: "order",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                schema: "order",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                schema: "order",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_DeliveryZoneId",
                schema: "order",
                table: "Orders",
                column: "DeliveryZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ExpectedReadyAt",
                schema: "order",
                table: "Orders",
                column: "ExpectedReadyAt");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_KitchenSlotStart",
                schema: "order",
                table: "Orders",
                column: "KitchenSlotStart");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_DeliveryZoneId",
                schema: "order",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ExpectedReadyAt",
                schema: "order",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_KitchenSlotStart",
                schema: "order",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "AcceptedAt",
                schema: "order",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CourierName",
                schema: "order",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CourierPhone",
                schema: "order",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryFeeMultiplier",
                schema: "order",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryPickupSlaMinutes",
                schema: "order",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryTransitSlaMinutes",
                schema: "order",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryZoneDistanceKm",
                schema: "order",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryZoneId",
                schema: "order",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryZoneName",
                schema: "order",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ExpectedReadyAt",
                schema: "order",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IsReadyForDelivery",
                schema: "order",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "KitchenDelayedNotifiedAt",
                schema: "order",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "KitchenSlotStart",
                schema: "order",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ReadyAt",
                schema: "order",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RejectedAt",
                schema: "order",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                schema: "order",
                table: "Orders");
        }
    }
}
