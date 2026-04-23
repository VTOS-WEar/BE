using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VTOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropCampaignTablesAndOrderCampaignFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Complaints_Campaign_CampaignID",
                table: "Complaints");

            migrationBuilder.DropForeignKey(
                name: "FK_Feedback_Campaign_CampaignId",
                table: "Feedback");

            migrationBuilder.DropForeignKey(
                name: "FK_Order_Campaign_CampaignID",
                table: "Order");

            migrationBuilder.DropTable(
                name: "CampaignOutfit");

            migrationBuilder.DropTable(
                name: "Campaign");

            migrationBuilder.DropIndex(
                name: "IX_Order_CampaignID",
                table: "Order");

            migrationBuilder.DropIndex(
                name: "IX_Feedback_CampaignId",
                table: "Feedback");

            migrationBuilder.DropIndex(
                name: "IX_Complaints_CampaignID",
                table: "Complaints");

            migrationBuilder.DropColumn(
                name: "CampaignID",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "CampaignId",
                table: "Feedback");

            migrationBuilder.DropColumn(
                name: "CampaignID",
                table: "Complaints");

            migrationBuilder.AddColumn<string>(
                name: "AppliedPricingMode",
                table: "Order",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrderID",
                table: "Complaints",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SemesterPublicationID",
                table: "Complaints",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProviderCatalogItem",
                columns: table => new
                {
                    ProviderCatalogItemID = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderID = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractItemID = table.Column<Guid>(type: "uuid", nullable: false),
                    OutfitID = table.Column<Guid>(type: "uuid", nullable: false),
                    SemesterPublicationProviderID = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ShortDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FullDescription = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    MaterialDetails = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CareInstructions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    MainImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    GalleryImageUrls = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    PublicationPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PostDeadlinePrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    HiddenAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderCatalogItem", x => x.ProviderCatalogItemID);
                    table.ForeignKey(
                        name: "FK_ProviderCatalogItem_ContractItem_ContractItemID",
                        column: x => x.ContractItemID,
                        principalTable: "ContractItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProviderCatalogItem_Outfit_OutfitID",
                        column: x => x.OutfitID,
                        principalTable: "Outfit",
                        principalColumn: "OutfitID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProviderCatalogItem_Provider_ProviderID",
                        column: x => x.ProviderID,
                        principalTable: "Provider",
                        principalColumn: "ProviderID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProviderCatalogItem_SemesterPublicationProvider_SemesterPub~",
                        column: x => x.SemesterPublicationProviderID,
                        principalTable: "SemesterPublicationProvider",
                        principalColumn: "SemesterPublicationProviderID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_OrderID",
                table: "Complaints",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_SemesterPublicationID",
                table: "Complaints",
                column: "SemesterPublicationID");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderCatalogItem_ContractItemID",
                table: "ProviderCatalogItem",
                column: "ContractItemID");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderCatalogItem_OutfitID",
                table: "ProviderCatalogItem",
                column: "OutfitID");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderCatalogItem_ProviderID",
                table: "ProviderCatalogItem",
                column: "ProviderID");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderCatalogItem_SemesterPublicationProviderID_ContractI~",
                table: "ProviderCatalogItem",
                columns: new[] { "SemesterPublicationProviderID", "ContractItemID" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Complaints_Order_OrderID",
                table: "Complaints",
                column: "OrderID",
                principalTable: "Order",
                principalColumn: "OrderID");

            migrationBuilder.AddForeignKey(
                name: "FK_Complaints_SemesterPublication_SemesterPublicationID",
                table: "Complaints",
                column: "SemesterPublicationID",
                principalTable: "SemesterPublication",
                principalColumn: "SemesterPublicationID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Complaints_Order_OrderID",
                table: "Complaints");

            migrationBuilder.DropForeignKey(
                name: "FK_Complaints_SemesterPublication_SemesterPublicationID",
                table: "Complaints");

            migrationBuilder.DropTable(
                name: "ProviderCatalogItem");

            migrationBuilder.DropIndex(
                name: "IX_Complaints_OrderID",
                table: "Complaints");

            migrationBuilder.DropIndex(
                name: "IX_Complaints_SemesterPublicationID",
                table: "Complaints");

            migrationBuilder.DropColumn(
                name: "AppliedPricingMode",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "OrderID",
                table: "Complaints");

            migrationBuilder.DropColumn(
                name: "SemesterPublicationID",
                table: "Complaints");

            migrationBuilder.AddColumn<Guid>(
                name: "CampaignID",
                table: "Order",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CampaignId",
                table: "Feedback",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CampaignID",
                table: "Complaints",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Campaign",
                columns: table => new
                {
                    CampaignID = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolID = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campaign", x => x.CampaignID);
                    table.ForeignKey(
                        name: "FK_Campaign_School_SchoolID",
                        column: x => x.SchoolID,
                        principalTable: "School",
                        principalColumn: "SchoolID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CampaignOutfit",
                columns: table => new
                {
                    CampaignOutfitID = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignID = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractID = table.Column<Guid>(type: "uuid", nullable: true),
                    OutfitID = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderID = table.Column<Guid>(type: "uuid", nullable: true),
                    CampaignPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MaxQuantity = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignOutfit", x => x.CampaignOutfitID);
                    table.ForeignKey(
                        name: "FK_CampaignOutfit_Campaign_CampaignID",
                        column: x => x.CampaignID,
                        principalTable: "Campaign",
                        principalColumn: "CampaignID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CampaignOutfit_Contract_ContractID",
                        column: x => x.ContractID,
                        principalTable: "Contract",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CampaignOutfit_Outfit_OutfitID",
                        column: x => x.OutfitID,
                        principalTable: "Outfit",
                        principalColumn: "OutfitID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CampaignOutfit_Provider_ProviderID",
                        column: x => x.ProviderID,
                        principalTable: "Provider",
                        principalColumn: "ProviderID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Order_CampaignID",
                table: "Order",
                column: "CampaignID");

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_CampaignId",
                table: "Feedback",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_CampaignID",
                table: "Complaints",
                column: "CampaignID");

            migrationBuilder.CreateIndex(
                name: "IX_Campaign_SchoolID",
                table: "Campaign",
                column: "SchoolID");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignOutfit_CampaignID",
                table: "CampaignOutfit",
                column: "CampaignID");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignOutfit_ContractID",
                table: "CampaignOutfit",
                column: "ContractID");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignOutfit_OutfitID",
                table: "CampaignOutfit",
                column: "OutfitID");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignOutfit_ProviderID",
                table: "CampaignOutfit",
                column: "ProviderID");

            migrationBuilder.AddForeignKey(
                name: "FK_Complaints_Campaign_CampaignID",
                table: "Complaints",
                column: "CampaignID",
                principalTable: "Campaign",
                principalColumn: "CampaignID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Feedback_Campaign_CampaignId",
                table: "Feedback",
                column: "CampaignId",
                principalTable: "Campaign",
                principalColumn: "CampaignID");

            migrationBuilder.AddForeignKey(
                name: "FK_Order_Campaign_CampaignID",
                table: "Order",
                column: "CampaignID",
                principalTable: "Campaign",
                principalColumn: "CampaignID",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
