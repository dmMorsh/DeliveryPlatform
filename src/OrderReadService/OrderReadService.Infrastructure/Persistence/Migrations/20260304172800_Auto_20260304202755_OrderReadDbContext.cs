using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderReadService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Auto_20260304202755_OrderReadDbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "order_read");

            migrationBuilder.CreateTable(
                name: "Orders",
                schema: "order_read",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourierId = table.Column<Guid>(type: "uuid", nullable: true),
                    CourierName = table.Column<string>(type: "text", nullable: true),
                    CourierPhone = table.Column<string>(type: "text", nullable: true),
                    FromAddress = table.Column<string>(type: "text", nullable: false),
                    ToAddress = table.Column<string>(type: "text", nullable: false),
                    FromLatitude = table.Column<double>(type: "double precision", nullable: false),
                    FromLongitude = table.Column<double>(type: "double precision", nullable: false),
                    ToLatitude = table.Column<double>(type: "double precision", nullable: false),
                    ToLongitude = table.Column<double>(type: "double precision", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    WeightGrams = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CostCents = table.Column<long>(type: "bigint", nullable: false),
                    Currency = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    CourierNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReadyAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsReadyForDelivery = table.Column<bool>(type: "boolean", nullable: false),
                    EstimatedDeliveryAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EstimatedArrivalMinutes = table.Column<int>(type: "integer", nullable: true),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    ExpectedReadyAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    KitchenSlotStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    KitchenDelayedNotifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveryZoneId = table.Column<string>(type: "text", nullable: true),
                    DeliveryZoneName = table.Column<string>(type: "text", nullable: true),
                    DeliveryZoneDistanceKm = table.Column<double>(type: "double precision", nullable: true),
                    DeliveryPickupSlaMinutes = table.Column<int>(type: "integer", nullable: true),
                    DeliveryTransitSlaMinutes = table.Column<int>(type: "integer", nullable: true),
                    DeliveryFeeMultiplier = table.Column<double>(type: "double precision", nullable: true),
                    KitchenSlotCounted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProcessedEvents",
                schema: "order_read",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    EventType = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Topic = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Partition = table.Column<int>(type: "integer", nullable: false),
                    Offset = table.Column<long>(type: "bigint", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Error = table.Column<string>(type: "text", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                schema: "order_read",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    PriceCents = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "order_read",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId",
                schema: "order_read",
                table: "OrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ClientId",
                schema: "order_read",
                table: "Orders",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CourierId",
                schema: "order_read",
                table: "Orders",
                column: "CourierId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CourierName",
                schema: "order_read",
                table: "Orders",
                column: "CourierName");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CreatedAt",
                schema: "order_read",
                table: "Orders",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_DeliveryZoneId",
                schema: "order_read",
                table: "Orders",
                column: "DeliveryZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ExpectedReadyAt",
                schema: "order_read",
                table: "Orders",
                column: "ExpectedReadyAt");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_KitchenSlotStart",
                schema: "order_read",
                table: "Orders",
                column: "KitchenSlotStart");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status",
                schema: "order_read",
                table: "Orders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedEvents_EventId",
                schema: "order_read",
                table: "ProcessedEvents",
                column: "EventId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderItems",
                schema: "order_read");

            migrationBuilder.DropTable(
                name: "ProcessedEvents",
                schema: "order_read");

            migrationBuilder.DropTable(
                name: "Orders",
                schema: "order_read");
        }
    }
}
