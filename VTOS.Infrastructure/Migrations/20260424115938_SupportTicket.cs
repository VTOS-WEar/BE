using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VTOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SupportTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Complaints_Order_OrderID",
                table: "Complaints");

            migrationBuilder.DropForeignKey(
                name: "FK_Complaints_Provider_ProviderID",
                table: "Complaints");

            migrationBuilder.DropForeignKey(
                name: "FK_Complaints_School_SchoolID",
                table: "Complaints");

            migrationBuilder.DropForeignKey(
                name: "FK_Complaints_SemesterPublication_SemesterPublicationID",
                table: "Complaints");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Complaints",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<Guid>(
                name: "SchoolID",
                table: "Complaints",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "Response",
                table: "Complaints",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Complaints",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Complaints",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RequesterEmail",
                table: "Complaints",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RequesterName",
                table: "Complaints",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RequesterRole",
                table: "Complaints",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "RequesterUserID",
                table: "Complaints",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_RequesterUserID_CreatedAt",
                table: "Complaints",
                columns: new[] { "RequesterUserID", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_Status_CreatedAt",
                table: "Complaints",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Complaints_Order_OrderID",
                table: "Complaints",
                column: "OrderID",
                principalTable: "Order",
                principalColumn: "OrderID",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Complaints_Provider_ProviderID",
                table: "Complaints",
                column: "ProviderID",
                principalTable: "Provider",
                principalColumn: "ProviderID",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Complaints_School_SchoolID",
                table: "Complaints",
                column: "SchoolID",
                principalTable: "School",
                principalColumn: "SchoolID",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Complaints_SemesterPublication_SemesterPublicationID",
                table: "Complaints",
                column: "SemesterPublicationID",
                principalTable: "SemesterPublication",
                principalColumn: "SemesterPublicationID",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Complaints_User_RequesterUserID",
                table: "Complaints",
                column: "RequesterUserID",
                principalTable: "User",
                principalColumn: "UserID",
                onDelete: ReferentialAction.SetNull);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Complaints_Order_OrderID",
                table: "Complaints");

            migrationBuilder.DropForeignKey(
                name: "FK_Complaints_Provider_ProviderID",
                table: "Complaints");

            migrationBuilder.DropForeignKey(
                name: "FK_Complaints_School_SchoolID",
                table: "Complaints");

            migrationBuilder.DropForeignKey(
                name: "FK_Complaints_SemesterPublication_SemesterPublicationID",
                table: "Complaints");

            migrationBuilder.DropForeignKey(
                name: "FK_Complaints_User_RequesterUserID",
                table: "Complaints");

            migrationBuilder.DropIndex(
                name: "IX_Complaints_RequesterUserID_CreatedAt",
                table: "Complaints");

            migrationBuilder.DropIndex(
                name: "IX_Complaints_Status_CreatedAt",
                table: "Complaints");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Complaints");

            migrationBuilder.DropColumn(
                name: "RequesterEmail",
                table: "Complaints");

            migrationBuilder.DropColumn(
                name: "RequesterName",
                table: "Complaints");

            migrationBuilder.DropColumn(
                name: "RequesterRole",
                table: "Complaints");

            migrationBuilder.DropColumn(
                name: "RequesterUserID",
                table: "Complaints");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Complaints",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<Guid>(
                name: "SchoolID",
                table: "Complaints",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Response",
                table: "Complaints",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Complaints",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AddForeignKey(
                name: "FK_Complaints_Order_OrderID",
                table: "Complaints",
                column: "OrderID",
                principalTable: "Order",
                principalColumn: "OrderID");

            migrationBuilder.AddForeignKey(
                name: "FK_Complaints_Provider_ProviderID",
                table: "Complaints",
                column: "ProviderID",
                principalTable: "Provider",
                principalColumn: "ProviderID");

            migrationBuilder.AddForeignKey(
                name: "FK_Complaints_School_SchoolID",
                table: "Complaints",
                column: "SchoolID",
                principalTable: "School",
                principalColumn: "SchoolID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Complaints_SemesterPublication_SemesterPublicationID",
                table: "Complaints",
                column: "SemesterPublicationID",
                principalTable: "SemesterPublication",
                principalColumn: "SemesterPublicationID");
        }
    }
}
