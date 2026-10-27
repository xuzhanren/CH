using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CanHappy.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddListingContactAndEstateType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContactName",
                table: "Listing",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                table: "Listing",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowContactInd",
                table: "Listing",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "EstateType",
                columns: table => new
                {
                    EstitateTypeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DeletedInd = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstateType", x => x.EstitateTypeId);
                });

            migrationBuilder.InsertData(
                table: "EstateType",
                columns: new[] { "EstitateTypeId", "CreatedBy", "CreatedDate", "Description", "ModifiedBy", "ModifiedDate", "Name" },
                values: new object[,]
                {
                    { 1, "system", new DateTime(2026, 2, 26, 0, 0, 0, 0, DateTimeKind.Utc), "House", null, null, "House" },
                    { 2, "system", new DateTime(2026, 2, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Bungalow", null, null, "Bungalow" },
                    { 3, "system", new DateTime(2026, 2, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Townhouse", null, null, "Townhouse" },
                    { 4, "system", new DateTime(2026, 2, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Apartment/Condo", null, null, "Apartment/Condo" },
                    { 5, "system", new DateTime(2026, 2, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Duplex", null, null, "Duplex" },
                    { 6, "system", new DateTime(2026, 2, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Land", null, null, "Land" },
                    { 7, "system", new DateTime(2026, 2, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Commerical/Office Space", null, null, "Commerical/Office Space" },
                    { 8, "system", new DateTime(2026, 2, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Parking", null, null, "Parking" },
                    { 9, "system", new DateTime(2026, 2, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Storage", null, null, "Storage" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_EstateType_Name",
                table: "EstateType",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EstateType");

            migrationBuilder.DropColumn(
                name: "ContactName",
                table: "Listing");

            migrationBuilder.DropColumn(
                name: "ContactPhone",
                table: "Listing");

            migrationBuilder.DropColumn(
                name: "ShowContactInd",
                table: "Listing");
        }
    }
}
