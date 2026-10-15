using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "inventoryservice");

            migrationBuilder.CreateTable(
                name: "Products",
                schema: "inventoryservice",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    ProductCode = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VendingTransactions",
                schema: "inventoryservice",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    TransactionId = table.Column<string>(type: "text", nullable: false),
                    MachineId = table.Column<string>(type: "text", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    SlotNumber = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CorrelationId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendingTransactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MachineInventories",
                schema: "inventoryservice",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    MachineId = table.Column<string>(type: "text", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    SlotNumber = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    LowStockThreshold = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MachineInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MachineInventories_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "inventoryservice",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MachineInventories_MachineId_SlotNumber",
                schema: "inventoryservice",
                table: "MachineInventories",
                columns: new[] { "MachineId", "SlotNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MachineInventories_ProductId",
                schema: "inventoryservice",
                table: "MachineInventories",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProductCode",
                schema: "inventoryservice",
                table: "Products",
                column: "ProductCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendingTransactions_TransactionId",
                schema: "inventoryservice",
                table: "VendingTransactions",
                column: "TransactionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MachineInventories",
                schema: "inventoryservice");

            migrationBuilder.DropTable(
                name: "VendingTransactions",
                schema: "inventoryservice");

            migrationBuilder.DropTable(
                name: "Products",
                schema: "inventoryservice");
        }
    }
}
