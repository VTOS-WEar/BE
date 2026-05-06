using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VTOS.Infrastructure.Persistence;

#nullable disable

namespace VTOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(VTOSDbContext))]
    [Migration("20260505170000_AddTryOnJobStatus")]
    public partial class AddTryOnJobStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "TryOnHistory",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "TryOnHistory",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "TryOnHistory",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.Sql("""
                UPDATE "TryOnHistory"
                SET "CompletedAt" = "TryOnTimestamp"
                WHERE "CompletedAt" IS NULL
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "TryOnHistory");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "TryOnHistory");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "TryOnHistory");
        }
    }
}
