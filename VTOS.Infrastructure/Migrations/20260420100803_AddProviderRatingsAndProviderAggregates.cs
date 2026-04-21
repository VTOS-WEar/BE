using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VTOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderRatingsAndProviderAggregates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AverageRating",
                table: "Provider",
                type: "numeric(4,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TotalCompletedOrders",
                table: "Provider",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalRatings",
                table: "Provider",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ProviderRating",
                columns: table => new
                {
                    ProviderRatingID = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderID = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderID = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentUserID = table.Column<Guid>(type: "uuid", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderRating", x => x.ProviderRatingID);
                    table.ForeignKey(
                        name: "FK_ProviderRating_Order_OrderID",
                        column: x => x.OrderID,
                        principalTable: "Order",
                        principalColumn: "OrderID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProviderRating_Provider_ProviderID",
                        column: x => x.ProviderID,
                        principalTable: "Provider",
                        principalColumn: "ProviderID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProviderRating_User_ParentUserID",
                        column: x => x.ParentUserID,
                        principalTable: "User",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderRating_OrderID_ParentUserID",
                table: "ProviderRating",
                columns: new[] { "OrderID", "ParentUserID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProviderRating_ParentUserID",
                table: "ProviderRating",
                column: "ParentUserID");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderRating_ProviderID",
                table: "ProviderRating",
                column: "ProviderID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProviderRating");

            migrationBuilder.DropColumn(
                name: "AverageRating",
                table: "Provider");

            migrationBuilder.DropColumn(
                name: "TotalCompletedOrders",
                table: "Provider");

            migrationBuilder.DropColumn(
                name: "TotalRatings",
                table: "Provider");
        }
    }
}
