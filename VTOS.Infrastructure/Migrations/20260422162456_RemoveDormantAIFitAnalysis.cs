using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VTOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDormantAIFitAnalysis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AIFitAnalysis");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AIFitAnalysis",
                columns: table => new
                {
                    AnalysisID = table.Column<Guid>(type: "uuid", nullable: false),
                    TryOnID = table.Column<Guid>(type: "uuid", nullable: false),
                    AlgorithmVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    DetectedBodyProportions = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FitScore = table.Column<int>(type: "integer", nullable: true),
                    SuggestedSize = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIFitAnalysis", x => x.AnalysisID);
                    table.ForeignKey(
                        name: "FK_AIFitAnalysis_TryOnHistory_TryOnID",
                        column: x => x.TryOnID,
                        principalTable: "TryOnHistory",
                        principalColumn: "TryOnID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AIFitAnalysis_TryOnID",
                table: "AIFitAnalysis",
                column: "TryOnID",
                unique: true);
        }
    }
}
