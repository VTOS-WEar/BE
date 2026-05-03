using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VTOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTermsAcceptance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ImageConsentAcceptedAt",
                table: "User",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TermsAcceptedAt",
                table: "User",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TermsVersion",
                table: "User",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ImageConsentAcceptedAt",
                table: "AccountRequests",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TermsAcceptedAt",
                table: "AccountRequests",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TermsVersion",
                table: "AccountRequests",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageConsentAcceptedAt",
                table: "User");

            migrationBuilder.DropColumn(
                name: "TermsAcceptedAt",
                table: "User");

            migrationBuilder.DropColumn(
                name: "TermsVersion",
                table: "User");

            migrationBuilder.DropColumn(
                name: "ImageConsentAcceptedAt",
                table: "AccountRequests");

            migrationBuilder.DropColumn(
                name: "TermsAcceptedAt",
                table: "AccountRequests");

            migrationBuilder.DropColumn(
                name: "TermsVersion",
                table: "AccountRequests");
        }
    }
}
