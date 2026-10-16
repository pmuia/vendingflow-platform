using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseQuantityAndItemLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerPhone",
                schema: "inventoryservice",
                table: "VendingTransactions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                schema: "inventoryservice",
                table: "VendingTransactions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                schema: "inventoryservice",
                table: "VendingTransactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ItemLedgerEntries",
                schema: "inventoryservice",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    MachineId = table.Column<string>(type: "text", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    TransactionId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MovementType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    QuantityChange = table.Column<int>(type: "integer", nullable: false),
                    BalanceAfter = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemLedgerEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemLedgerEntries_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "inventoryservice",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemLedgerEntries_MachineId_CreatedAt",
                schema: "inventoryservice",
                table: "ItemLedgerEntries",
                columns: new[] { "MachineId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemLedgerEntries_ProductId",
                schema: "inventoryservice",
                table: "ItemLedgerEntries",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemLedgerEntries_TransactionId",
                schema: "inventoryservice",
                table: "ItemLedgerEntries",
                column: "TransactionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemLedgerEntries",
                schema: "inventoryservice");

            migrationBuilder.DropColumn(
                name: "CustomerPhone",
                schema: "inventoryservice",
                table: "VendingTransactions");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                schema: "inventoryservice",
                table: "VendingTransactions");

            migrationBuilder.DropColumn(
                name: "Quantity",
                schema: "inventoryservice",
                table: "VendingTransactions");
        }
    }
}
