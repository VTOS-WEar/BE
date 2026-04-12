using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VTOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBodygramTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BodygramScanLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChildId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomScanId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BodygramScanId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BodygramScanLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BodygramScanLogs_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "ChildID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BodygramScanRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChildId = table.Column<Guid>(type: "uuid", nullable: false),
                    BodygramScanId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CustomScanId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ScannedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUnix = table.Column<long>(type: "bigint", nullable: false),
                    HeightCm = table.Column<int>(type: "integer", nullable: false),
                    WeightKg = table.Column<float>(type: "real", nullable: false),
                    AvatarUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AvatarFormat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AvatarType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RawInputJson = table.Column<string>(type: "text", nullable: true),
                    RawMeasurementsJson = table.Column<string>(type: "text", nullable: true),
                    WaistToHipRatio = table.Column<double>(type: "double precision", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BodygramScanRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BodygramScanRecords_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "ChildID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BodygramMeasurementRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScanRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Value = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BodygramMeasurementRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BodygramMeasurementRecords_BodygramScanRecords_ScanRecordId",
                        column: x => x.ScanRecordId,
                        principalTable: "BodygramScanRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BodygramMeasurementRecords_ScanRecordId_Name",
                table: "BodygramMeasurementRecords",
                columns: new[] { "ScanRecordId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_BodygramScanLogs_ChildId",
                table: "BodygramScanLogs",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_BodygramScanRecords_BodygramScanId",
                table: "BodygramScanRecords",
                column: "BodygramScanId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BodygramScanRecords_ChildId",
                table: "BodygramScanRecords",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_BodygramScanRecords_CustomScanId",
                table: "BodygramScanRecords",
                column: "CustomScanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BodygramMeasurementRecords");

            migrationBuilder.DropTable(
                name: "BodygramScanLogs");

            migrationBuilder.DropTable(
                name: "BodygramScanRecords");
        }
    }
}
