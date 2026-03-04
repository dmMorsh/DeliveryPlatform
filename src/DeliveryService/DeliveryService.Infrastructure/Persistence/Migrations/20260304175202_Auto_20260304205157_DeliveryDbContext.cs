using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeliveryService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Auto_20260304205157_DeliveryDbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "DeliveryFeeMultiplier",
                schema: "delivery",
                table: "Deliveries",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryPickupSlaMinutes",
                schema: "delivery",
                table: "Deliveries",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryTransitSlaMinutes",
                schema: "delivery",
                table: "Deliveries",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DeliveryZoneDistanceKm",
                schema: "delivery",
                table: "Deliveries",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryZoneId",
                schema: "delivery",
                table: "Deliveries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryZoneName",
                schema: "delivery",
                table: "Deliveries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EstimatedDeliveryAt",
                schema: "delivery",
                table: "Deliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EstimatedDistanceKm",
                schema: "delivery",
                table: "Deliveries",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EstimatedPickupAt",
                schema: "delivery",
                table: "Deliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedTravelMinutes",
                schema: "delivery",
                table: "Deliveries",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastReassignAt",
                schema: "delivery",
                table: "Deliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PickupTimeoutNotifiedAt",
                schema: "delivery",
                table: "Deliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReassignAttempts",
                schema: "delivery",
                table: "Deliveries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "TransitTimeoutNotifiedAt",
                schema: "delivery",
                table: "Deliveries",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryFeeMultiplier",
                schema: "delivery",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "DeliveryPickupSlaMinutes",
                schema: "delivery",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "DeliveryTransitSlaMinutes",
                schema: "delivery",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "DeliveryZoneDistanceKm",
                schema: "delivery",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "DeliveryZoneId",
                schema: "delivery",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "DeliveryZoneName",
                schema: "delivery",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "EstimatedDeliveryAt",
                schema: "delivery",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "EstimatedDistanceKm",
                schema: "delivery",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "EstimatedPickupAt",
                schema: "delivery",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "EstimatedTravelMinutes",
                schema: "delivery",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "LastReassignAt",
                schema: "delivery",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "PickupTimeoutNotifiedAt",
                schema: "delivery",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "ReassignAttempts",
                schema: "delivery",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "TransitTimeoutNotifiedAt",
                schema: "delivery",
                table: "Deliveries");
        }
    }
}
