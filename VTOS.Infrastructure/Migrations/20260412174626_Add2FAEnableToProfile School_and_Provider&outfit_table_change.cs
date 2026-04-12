using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VTOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add2FAEnableToProfileSchoolandProvideroutfittablechange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChestMax",
                table: "SizeChartDetail");

            migrationBuilder.DropColumn(
                name: "ChestMin",
                table: "SizeChartDetail");

            migrationBuilder.DropColumn(
                name: "HeightMax",
                table: "SizeChartDetail");

            migrationBuilder.DropColumn(
                name: "HeightMin",
                table: "SizeChartDetail");

            migrationBuilder.DropColumn(
                name: "HipMax",
                table: "SizeChartDetail");

            migrationBuilder.DropColumn(
                name: "HipMin",
                table: "SizeChartDetail");

            migrationBuilder.DropColumn(
                name: "OtherMeasurements",
                table: "SizeChartDetail");

            migrationBuilder.DropColumn(
                name: "WaistMax",
                table: "SizeChartDetail");

            migrationBuilder.DropColumn(
                name: "WaistMin",
                table: "SizeChartDetail");

            migrationBuilder.CreateTable(
                name: "SizeChartMeasurement",
                columns: table => new
                {
                    MeasurementID = table.Column<Guid>(type: "uuid", nullable: false),
                    SizeChartDetailId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Unit = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "cm"),
                    MinCm = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    MaxCm = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SizeChartMeasurement", x => x.MeasurementID);
                    table.ForeignKey(
                        name: "FK_SizeChartMeasurement_SizeChartDetail_SizeChartDetailId",
                        column: x => x.SizeChartDetailId,
                        principalTable: "SizeChartDetail",
                        principalColumn: "DetailID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SizeChartMeasurement_SizeChartDetailId",
                table: "SizeChartMeasurement",
                column: "SizeChartDetailId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SizeChartMeasurement");

            migrationBuilder.AddColumn<decimal>(
                name: "ChestMax",
                table: "SizeChartDetail",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ChestMin",
                table: "SizeChartDetail",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HeightMax",
                table: "SizeChartDetail",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HeightMin",
                table: "SizeChartDetail",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HipMax",
                table: "SizeChartDetail",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HipMin",
                table: "SizeChartDetail",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtherMeasurements",
                table: "SizeChartDetail",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WaistMax",
                table: "SizeChartDetail",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WaistMin",
                table: "SizeChartDetail",
                type: "numeric(5,2)",
                nullable: true);
        }
    }
}
