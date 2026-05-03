using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VTOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSecureTryOnImageObjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UploadedPhotoURL",
                table: "TryOnHistory",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<string>(
                name: "ResultPhotoContentType",
                table: "TryOnHistory",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResultPhotoObjectKey",
                table: "TryOnHistory",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ResultPhotoSizeBytes",
                table: "TryOnHistory",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UploadedPhotoContentType",
                table: "TryOnHistory",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UploadedPhotoObjectKey",
                table: "TryOnHistory",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "UploadedPhotoSizeBytes",
                table: "TryOnHistory",
                type: "bigint",
                nullable: true);
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ResultPhotoContentType", table: "TryOnHistory");
            migrationBuilder.DropColumn(name: "ResultPhotoObjectKey", table: "TryOnHistory");
            migrationBuilder.DropColumn(name: "ResultPhotoSizeBytes", table: "TryOnHistory");
            migrationBuilder.DropColumn(name: "UploadedPhotoContentType", table: "TryOnHistory");
            migrationBuilder.DropColumn(name: "UploadedPhotoObjectKey", table: "TryOnHistory");
            migrationBuilder.DropColumn(name: "UploadedPhotoSizeBytes", table: "TryOnHistory");

            migrationBuilder.AlterColumn<string>(
                name: "UploadedPhotoURL",
                table: "TryOnHistory",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }


    }
}
