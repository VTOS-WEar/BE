using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VTOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClassGroupAndTeacherImport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HomeroomTeacherEmail",
                table: "StudentDataImport",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HomeroomTeacherName",
                table: "StudentDataImport",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClassGroupID",
                table: "Children",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClassGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolID = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Grade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AcademicYear = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    HomeroomTeacherID = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassGroups_School_SchoolID",
                        column: x => x.SchoolID,
                        principalTable: "School",
                        principalColumn: "SchoolID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassGroups_User_HomeroomTeacherID",
                        column: x => x.HomeroomTeacherID,
                        principalTable: "User",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Children_ClassGroupID",
                table: "Children",
                column: "ClassGroupID");

            migrationBuilder.CreateIndex(
                name: "IX_ClassGroups_HomeroomTeacherID",
                table: "ClassGroups",
                column: "HomeroomTeacherID");

            migrationBuilder.CreateIndex(
                name: "IX_ClassGroups_SchoolID_ClassName_AcademicYear",
                table: "ClassGroups",
                columns: new[] { "SchoolID", "ClassName", "AcademicYear" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Children_ClassGroups_ClassGroupID",
                table: "Children",
                column: "ClassGroupID",
                principalTable: "ClassGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Children_ClassGroups_ClassGroupID",
                table: "Children");

            migrationBuilder.DropTable(
                name: "ClassGroups");

            migrationBuilder.DropIndex(
                name: "IX_Children_ClassGroupID",
                table: "Children");

            migrationBuilder.DropColumn(
                name: "HomeroomTeacherEmail",
                table: "StudentDataImport");

            migrationBuilder.DropColumn(
                name: "HomeroomTeacherName",
                table: "StudentDataImport");

            migrationBuilder.DropColumn(
                name: "ClassGroupID",
                table: "Children");
        }
    }
}
