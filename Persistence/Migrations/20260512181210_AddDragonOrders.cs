using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDragonOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DragonOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CustomerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CustomerName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CustomerContact = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IsOwnOrder = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsClubeDragon = table.Column<bool>(type: "INTEGER", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    FactoryCost = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ShippingCost = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TotalCost = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Margin = table.Column<decimal>(type: "TEXT", precision: 6, scale: 2, nullable: false),
                    CashbackAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TotalPaid = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    RemainingBalance = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    IsFullyPaid = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsFactoryPaid = table.Column<bool>(type: "INTEGER", nullable: false),
                    FactoryPaidAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    TrackingCode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DragonOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DragonOrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    BoxType = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitCost = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    SubtotalCost = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    SubtotalPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    DragonOrderId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DragonOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DragonOrderItems_DragonOrders_DragonOrderId",
                        column: x => x.DragonOrderId,
                        principalTable: "DragonOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DragonPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DragonOrderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Method = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PaidAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DragonPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DragonPayments_DragonOrders_DragonOrderId",
                        column: x => x.DragonOrderId,
                        principalTable: "DragonOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DragonOrderItems_DragonOrderId",
                table: "DragonOrderItems",
                column: "DragonOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_DragonPayments_DragonOrderId",
                table: "DragonPayments",
                column: "DragonOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DragonOrderItems");

            migrationBuilder.DropTable(
                name: "DragonPayments");

            migrationBuilder.DropTable(
                name: "DragonOrders");
        }
    }
}
