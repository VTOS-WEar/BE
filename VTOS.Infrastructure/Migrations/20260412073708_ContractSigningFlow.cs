using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VTOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ContractSigningFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "School",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "School",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepresentativeName",
                table: "School",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepresentativeTitle",
                table: "School",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxCode",
                table: "School",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepresentativeTitle",
                table: "Provider",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxCode",
                table: "Provider",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContractNumber",
                table: "Contract",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProviderSignature",
                table: "Contract",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProviderSignedAt",
                table: "Contract",
                type: "timestamp",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SchoolSignature",
                table: "Contract",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SchoolSignedAt",
                table: "Contract",
                type: "timestamp",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SigningOTPCode",
                table: "Contract",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SigningOTPExpiry",
                table: "Contract",
                type: "timestamp",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SigningOTPFor",
                table: "Contract",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "School");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "School");

            migrationBuilder.DropColumn(
                name: "RepresentativeName",
                table: "School");

            migrationBuilder.DropColumn(
                name: "RepresentativeTitle",
                table: "School");

            migrationBuilder.DropColumn(
                name: "TaxCode",
                table: "School");

            migrationBuilder.DropColumn(
                name: "RepresentativeTitle",
                table: "Provider");

            migrationBuilder.DropColumn(
                name: "TaxCode",
                table: "Provider");

            migrationBuilder.DropColumn(
                name: "ContractNumber",
                table: "Contract");

            migrationBuilder.DropColumn(
                name: "ProviderSignature",
                table: "Contract");

            migrationBuilder.DropColumn(
                name: "ProviderSignedAt",
                table: "Contract");

            migrationBuilder.DropColumn(
                name: "SchoolSignature",
                table: "Contract");

            migrationBuilder.DropColumn(
                name: "SchoolSignedAt",
                table: "Contract");

            migrationBuilder.DropColumn(
                name: "SigningOTPCode",
                table: "Contract");

            migrationBuilder.DropColumn(
                name: "SigningOTPExpiry",
                table: "Contract");

            migrationBuilder.DropColumn(
                name: "SigningOTPFor",
                table: "Contract");
        }
    }
}
