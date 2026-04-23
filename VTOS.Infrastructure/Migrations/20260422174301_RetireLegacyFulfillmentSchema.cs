using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VTOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RetireLegacyFulfillmentSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Complaints_ProductionBatch_BatchID",
                table: "Complaints");

            migrationBuilder.DropTable(
                name: "DeliveryRecord");

            migrationBuilder.DropTable(
                name: "DistributionRecord");

            migrationBuilder.DropTable(
                name: "DistributionSchedules");

            migrationBuilder.DropTable(
                name: "ProductionBatchItems");

            migrationBuilder.DropTable(
                name: "ProductionBatch");

            migrationBuilder.DropIndex(
                name: "IX_Complaints_BatchID",
                table: "Complaints");

            migrationBuilder.DropColumn(
                name: "BatchID",
                table: "Complaints");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BatchID",
                table: "Complaints",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductionBatch",
                columns: table => new
                {
                    BatchID = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignID = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderID = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DeliveredQuantity = table.Column<int>(type: "integer", nullable: false),
                    DeliveryConfirmedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeliveryDeadline = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeliveryNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", maxLength: 20, nullable: false),
                    TotalQuantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionBatch", x => x.BatchID);
                    table.ForeignKey(
                        name: "FK_ProductionBatch_Campaign_CampaignID",
                        column: x => x.CampaignID,
                        principalTable: "Campaign",
                        principalColumn: "CampaignID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionBatch_Provider_ProviderID",
                        column: x => x.ProviderID,
                        principalTable: "Provider",
                        principalColumn: "ProviderID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryRecord",
                columns: table => new
                {
                    DeliveryRecordID = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchID = table.Column<Guid>(type: "uuid", nullable: false),
                    AcceptedQuantity = table.Column<int>(type: "integer", nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    DefectNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DefectiveQuantity = table.Column<int>(type: "integer", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryRecord", x => x.DeliveryRecordID);
                    table.ForeignKey(
                        name: "FK_DeliveryRecord_ProductionBatch_BatchID",
                        column: x => x.BatchID,
                        principalTable: "ProductionBatch",
                        principalColumn: "BatchID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DistributionRecord",
                columns: table => new
                {
                    DistributionRecordID = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchID = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderID = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    DistributedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ProofImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ShippingCompany = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TrackingCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DistributionRecord", x => x.DistributionRecordID);
                    table.ForeignKey(
                        name: "FK_DistributionRecord_Order_OrderID",
                        column: x => x.OrderID,
                        principalTable: "Order",
                        principalColumn: "OrderID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DistributionRecord_ProductionBatch_BatchID",
                        column: x => x.BatchID,
                        principalTable: "ProductionBatch",
                        principalColumn: "BatchID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DistributionSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchID = table.Column<Guid>(type: "uuid", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Method = table.Column<string>(type: "text", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    ScheduledDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    TimeSlot = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DistributionSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DistributionSchedules_ProductionBatch_BatchID",
                        column: x => x.BatchID,
                        principalTable: "ProductionBatch",
                        principalColumn: "BatchID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductionBatchItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchID = table.Column<Guid>(type: "uuid", nullable: false),
                    OutfitID = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Size = table.Column<string>(type: "text", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionBatchItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionBatchItems_Outfit_OutfitID",
                        column: x => x.OutfitID,
                        principalTable: "Outfit",
                        principalColumn: "OutfitID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionBatchItems_ProductionBatch_BatchID",
                        column: x => x.BatchID,
                        principalTable: "ProductionBatch",
                        principalColumn: "BatchID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_BatchID",
                table: "Complaints",
                column: "BatchID");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryRecord_BatchID",
                table: "DeliveryRecord",
                column: "BatchID");

            migrationBuilder.CreateIndex(
                name: "IX_DistributionRecord_BatchID",
                table: "DistributionRecord",
                column: "BatchID");

            migrationBuilder.CreateIndex(
                name: "IX_DistributionRecord_OrderID",
                table: "DistributionRecord",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_DistributionSchedules_BatchID",
                table: "DistributionSchedules",
                column: "BatchID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBatch_CampaignID",
                table: "ProductionBatch",
                column: "CampaignID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBatch_IsDeleted",
                table: "ProductionBatch",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBatch_ProviderID",
                table: "ProductionBatch",
                column: "ProviderID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBatchItems_BatchID",
                table: "ProductionBatchItems",
                column: "BatchID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBatchItems_OutfitID",
                table: "ProductionBatchItems",
                column: "OutfitID");

            migrationBuilder.AddForeignKey(
                name: "FK_Complaints_ProductionBatch_BatchID",
                table: "Complaints",
                column: "BatchID",
                principalTable: "ProductionBatch",
                principalColumn: "BatchID");
        }
    }
}
