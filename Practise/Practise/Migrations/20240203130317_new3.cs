using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Storage.Core.Migrations
{
    /// <inheritdoc />
    public partial class new3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StorageTransaction_Products_ProductId",
                table: "StorageTransaction");

            migrationBuilder.DropForeignKey(
                name: "FK_StorageTransaction_TransactionType_TransactionTypeId",
                table: "StorageTransaction");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TransactionType",
                table: "TransactionType");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StorageTransaction",
                table: "StorageTransaction");

            migrationBuilder.RenameTable(
                name: "TransactionType",
                newName: "TransactionTypes");

            migrationBuilder.RenameTable(
                name: "StorageTransaction",
                newName: "StorageTransactions");

            migrationBuilder.RenameIndex(
                name: "IX_StorageTransaction_TransactionTypeId",
                table: "StorageTransactions",
                newName: "IX_StorageTransactions_TransactionTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_StorageTransaction_ProductId",
                table: "StorageTransactions",
                newName: "IX_StorageTransactions_ProductId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TransactionTypes",
                table: "TransactionTypes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StorageTransactions",
                table: "StorageTransactions",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderProductQuantity = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<float>(type: "real", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderDetails_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderDetails_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetails_OrderId",
                table: "OrderDetails",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetails_ProductId",
                table: "OrderDetails",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_StorageTransactions_Products_ProductId",
                table: "StorageTransactions",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StorageTransactions_TransactionTypes_TransactionTypeId",
                table: "StorageTransactions",
                column: "TransactionTypeId",
                principalTable: "TransactionTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StorageTransactions_Products_ProductId",
                table: "StorageTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_StorageTransactions_TransactionTypes_TransactionTypeId",
                table: "StorageTransactions");

            migrationBuilder.DropTable(
                name: "OrderDetails");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TransactionTypes",
                table: "TransactionTypes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StorageTransactions",
                table: "StorageTransactions");

            migrationBuilder.RenameTable(
                name: "TransactionTypes",
                newName: "TransactionType");

            migrationBuilder.RenameTable(
                name: "StorageTransactions",
                newName: "StorageTransaction");

            migrationBuilder.RenameIndex(
                name: "IX_StorageTransactions_TransactionTypeId",
                table: "StorageTransaction",
                newName: "IX_StorageTransaction_TransactionTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_StorageTransactions_ProductId",
                table: "StorageTransaction",
                newName: "IX_StorageTransaction_ProductId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TransactionType",
                table: "TransactionType",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StorageTransaction",
                table: "StorageTransaction",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StorageTransaction_Products_ProductId",
                table: "StorageTransaction",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StorageTransaction_TransactionType_TransactionTypeId",
                table: "StorageTransaction",
                column: "TransactionTypeId",
                principalTable: "TransactionType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
