using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFactoryOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FactoryOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CustomerName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CustomerContact = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DeliveryAddress = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    SupplierName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SupplierContact = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TotalCost = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TotalSalePrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Margin = table.Column<decimal>(type: "TEXT", precision: 6, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Channel = table.Column<int>(type: "INTEGER", nullable: false),
                    TrackingCode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactoryOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FactoryOrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitCost = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    UnitSalePrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    SubtotalCost = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    SubtotalSalePrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    FactoryOrderId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactoryOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FactoryOrderItems_FactoryOrders_FactoryOrderId",
                        column: x => x.FactoryOrderId,
                        principalTable: "FactoryOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FactoryOrderItems_FactoryOrderId",
                table: "FactoryOrderItems",
                column: "FactoryOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FactoryOrderItems");

            migrationBuilder.DropTable(
                name: "FactoryOrders");
        }
    }
}
