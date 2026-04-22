using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VTOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEscrowAndAutoPayout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EscrowStatus",
                table: "PaymentTransaction",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PayoutRecordID",
                table: "PaymentTransaction",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PayoutRecord",
                columns: table => new
                {
                    PayoutRecordID = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderID = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderID = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PayoutMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    AdminNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayoutRecord", x => x.PayoutRecordID);
                    table.ForeignKey(
                        name: "FK_PayoutRecord_Order_OrderID",
                        column: x => x.OrderID,
                        principalTable: "Order",
                        principalColumn: "OrderID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayoutRecord_Provider_ProviderID",
                        column: x => x.ProviderID,
                        principalTable: "Provider",
                        principalColumn: "ProviderID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransaction_PayoutRecordID",
                table: "PaymentTransaction",
                column: "PayoutRecordID");

            migrationBuilder.CreateIndex(
                name: "IX_PayoutRecord_OrderID",
                table: "PayoutRecord",
                column: "OrderID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayoutRecord_ProviderID",
                table: "PayoutRecord",
                column: "ProviderID");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentTransaction_PayoutRecord_PayoutRecordID",
                table: "PaymentTransaction",
                column: "PayoutRecordID",
                principalTable: "PayoutRecord",
                principalColumn: "PayoutRecordID",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentTransaction_PayoutRecord_PayoutRecordID",
                table: "PaymentTransaction");

            migrationBuilder.DropTable(
                name: "PayoutRecord");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTransaction_PayoutRecordID",
                table: "PaymentTransaction");

            migrationBuilder.DropColumn(
                name: "EscrowStatus",
                table: "PaymentTransaction");

            migrationBuilder.DropColumn(
                name: "PayoutRecordID",
                table: "PaymentTransaction");
        }
    }
}
