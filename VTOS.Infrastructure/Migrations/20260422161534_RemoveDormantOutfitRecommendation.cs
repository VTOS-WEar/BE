using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VTOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDormantOutfitRecommendation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutfitRecommendation");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OutfitRecommendation",
                columns: table => new
                {
                    RecommendationID = table.Column<Guid>(type: "uuid", nullable: false),
                    OutfitID = table.Column<Guid>(type: "uuid", nullable: false),
                    UserID = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    RecommendationScore = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    RuleConfigID = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutfitRecommendation", x => x.RecommendationID);
                    table.ForeignKey(
                        name: "FK_OutfitRecommendation_Outfit_OutfitID",
                        column: x => x.OutfitID,
                        principalTable: "Outfit",
                        principalColumn: "OutfitID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OutfitRecommendation_User_UserID",
                        column: x => x.UserID,
                        principalTable: "User",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutfitRecommendation_OutfitID",
                table: "OutfitRecommendation",
                column: "OutfitID");

            migrationBuilder.CreateIndex(
                name: "IX_OutfitRecommendation_UserID",
                table: "OutfitRecommendation",
                column: "UserID");
        }
    }
}
