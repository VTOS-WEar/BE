using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VTOS.Infrastructure.Persistence;

#nullable disable

namespace VTOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(VTOSDbContext))]
    [Migration("20260506130000_AddProviderCatalogVariantOwnership")]
    public partial class AddProviderCatalogVariantOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProviderCatalogItemID",
                table: "ProductVariant",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SizeChartID",
                table: "ProviderCatalogItem",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariant_ProviderCatalogItemID",
                table: "ProductVariant",
                column: "ProviderCatalogItemID");

            migrationBuilder.CreateIndex(
                name: "UX_ProductVariant_ProviderCatalogItem_Size_Active",
                table: "ProductVariant",
                columns: new[] { "ProviderCatalogItemID", "Size" },
                unique: true,
                filter: "\"ProviderCatalogItemID\" IS NOT NULL AND \"IsDeleted\" IS FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderCatalogItem_SizeChartID",
                table: "ProviderCatalogItem",
                column: "SizeChartID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductVariant_ProviderCatalogItem_ProviderCatalogItemID",
                table: "ProductVariant",
                column: "ProviderCatalogItemID",
                principalTable: "ProviderCatalogItem",
                principalColumn: "ProviderCatalogItemID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProviderCatalogItem_SizeChart_SizeChartID",
                table: "ProviderCatalogItem",
                column: "SizeChartID",
                principalTable: "SizeChart",
                principalColumn: "SizeChartID",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductVariant_ProviderCatalogItem_ProviderCatalogItemID",
                table: "ProductVariant");

            migrationBuilder.DropForeignKey(
                name: "FK_ProviderCatalogItem_SizeChart_SizeChartID",
                table: "ProviderCatalogItem");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariant_ProviderCatalogItemID",
                table: "ProductVariant");

            migrationBuilder.DropIndex(
                name: "UX_ProductVariant_ProviderCatalogItem_Size_Active",
                table: "ProductVariant");

            migrationBuilder.DropIndex(
                name: "IX_ProviderCatalogItem_SizeChartID",
                table: "ProviderCatalogItem");

            migrationBuilder.DropColumn(
                name: "ProviderCatalogItemID",
                table: "ProductVariant");

            migrationBuilder.DropColumn(
                name: "SizeChartID",
                table: "ProviderCatalogItem");
        }
    }
}
