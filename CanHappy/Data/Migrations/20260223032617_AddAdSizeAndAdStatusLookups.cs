using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CanHappy.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdSizeAndAdStatusLookups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdSize",
                columns: table => new
                {
                    AdSizeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DeletedInd = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdSize", x => x.AdSizeId);
                });

            migrationBuilder.CreateTable(
                name: "AdStatus",
                columns: table => new
                {
                    AdStatusId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DeletedInd = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdStatus", x => x.AdStatusId);
                });

            migrationBuilder.InsertData(
                table: "AdSize",
                columns: new[] { "AdSizeId", "CreatedBy", "CreatedDate", "Description", "ModifiedBy", "ModifiedDate", "Name" },
                values: new object[,]
                {
                    { 1, "system", new DateTime(2026, 2, 22, 0, 0, 0, 0, DateTimeKind.Utc), "Medium Rectangle", null, null, "300x250" },
                    { 2, "system", new DateTime(2026, 2, 22, 0, 0, 0, 0, DateTimeKind.Utc), "Mobile Banner", null, null, "320x50" },
                    { 3, "system", new DateTime(2026, 2, 22, 0, 0, 0, 0, DateTimeKind.Utc), "Leaderboard", null, null, "728x90" },
                    { 4, "system", new DateTime(2026, 2, 22, 0, 0, 0, 0, DateTimeKind.Utc), "Social Share Image", null, null, "1200x628" }
                });

            migrationBuilder.InsertData(
                table: "AdStatus",
                columns: new[] { "AdStatusId", "CreatedBy", "CreatedDate", "Description", "ModifiedBy", "ModifiedDate", "Name" },
                values: new object[,]
                {
                    { 1, "system", new DateTime(2026, 2, 22, 0, 0, 0, 0, DateTimeKind.Utc), "Work in progress", null, null, "Draft" },
                    { 2, "system", new DateTime(2026, 2, 22, 0, 0, 0, 0, DateTimeKind.Utc), "Creative approved", null, null, "Design finalized" },
                    { 3, "system", new DateTime(2026, 2, 22, 0, 0, 0, 0, DateTimeKind.Utc), "Active and visible", null, null, "Released to show" },
                    { 4, "system", new DateTime(2026, 2, 22, 0, 0, 0, 0, DateTimeKind.Utc), "Temporarily inactive", null, null, "Stopped from show" },
                    { 5, "system", new DateTime(2026, 2, 22, 0, 0, 0, 0, DateTimeKind.Utc), "Permanently retired", null, null, "Decommssioned" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdSize_Name",
                table: "AdSize",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdStatus_Name",
                table: "AdStatus",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdSize");

            migrationBuilder.DropTable(
                name: "AdStatus");
        }
    }
}
