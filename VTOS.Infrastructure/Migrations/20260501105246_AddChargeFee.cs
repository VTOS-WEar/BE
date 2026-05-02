using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VTOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChargeFee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "FeeAmount",
                table: "WalletWithdrawalRequest",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FeeRate",
                table: "WalletWithdrawalRequest",
                type: "numeric(5,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "NetAmount",
                table: "WalletWithdrawalRequest",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GrossAmount",
                table: "PayoutRecord",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "NetAmount",
                table: "PayoutRecord",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PlatformFeeAmount",
                table: "PayoutRecord",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PlatformFeeRate",
                table: "PayoutRecord",
                type: "numeric(5,4)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FeeAmount",
                table: "WalletWithdrawalRequest");

            migrationBuilder.DropColumn(
                name: "FeeRate",
                table: "WalletWithdrawalRequest");

            migrationBuilder.DropColumn(
                name: "NetAmount",
                table: "WalletWithdrawalRequest");

            migrationBuilder.DropColumn(
                name: "GrossAmount",
                table: "PayoutRecord");

            migrationBuilder.DropColumn(
                name: "NetAmount",
                table: "PayoutRecord");

            migrationBuilder.DropColumn(
                name: "PlatformFeeAmount",
                table: "PayoutRecord");

            migrationBuilder.DropColumn(
                name: "PlatformFeeRate",
                table: "PayoutRecord");
        }
    }
}
