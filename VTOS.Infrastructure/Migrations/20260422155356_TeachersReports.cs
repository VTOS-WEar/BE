using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VTOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TeachersReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MaterialType",
                table: "Outfit",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProviderID",
                table: "Order",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecipientName",
                table: "Order",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecipientPhone",
                table: "Order",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SemesterPublicationID",
                table: "Order",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingCompany",
                table: "Order",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrackingCode",
                table: "Order",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TeacherReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeacherUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ReviewNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeacherReports_ClassGroups_ClassGroupId",
                        column: x => x.ClassGroupId,
                        principalTable: "ClassGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeacherReports_User_TeacherUserId",
                        column: x => x.TeacherUserId,
                        principalTable: "User",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Order_ProviderID",
                table: "Order",
                column: "ProviderID");

            migrationBuilder.CreateIndex(
                name: "IX_Order_SemesterPublicationID",
                table: "Order",
                column: "SemesterPublicationID");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherReports_ClassGroupId_Status",
                table: "TeacherReports",
                columns: new[] { "ClassGroupId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TeacherReports_TeacherUserId_ClassGroupId_SubmittedAt",
                table: "TeacherReports",
                columns: new[] { "TeacherUserId", "ClassGroupId", "SubmittedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Order_Provider_ProviderID",
                table: "Order",
                column: "ProviderID",
                principalTable: "Provider",
                principalColumn: "ProviderID",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Order_SemesterPublication_SemesterPublicationID",
                table: "Order",
                column: "SemesterPublicationID",
                principalTable: "SemesterPublication",
                principalColumn: "SemesterPublicationID",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Order_Provider_ProviderID",
                table: "Order");

            migrationBuilder.DropForeignKey(
                name: "FK_Order_SemesterPublication_SemesterPublicationID",
                table: "Order");

            migrationBuilder.DropTable(
                name: "TeacherReports");

            migrationBuilder.DropIndex(
                name: "IX_Order_ProviderID",
                table: "Order");

            migrationBuilder.DropIndex(
                name: "IX_Order_SemesterPublicationID",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "MaterialType",
                table: "Outfit");

            migrationBuilder.DropColumn(
                name: "ProviderID",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "RecipientName",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "RecipientPhone",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "SemesterPublicationID",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "ShippingCompany",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "TrackingCode",
                table: "Order");
        }
    }
}
