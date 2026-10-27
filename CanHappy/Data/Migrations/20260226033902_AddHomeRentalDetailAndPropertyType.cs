using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CanHappy.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHomeRentalDetailAndPropertyType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PropertyType",
                columns: table => new
                {
                    PropertyTypeId = table.Column<int>(type: "integer", nullable: false)
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
                    table.PrimaryKey("PK_PropertyType", x => x.PropertyTypeId);
                });

            migrationBuilder.CreateTable(
                name: "HomeRentalDetail",
                columns: table => new
                {
                    HomeRentalDetailGUID = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingGUID = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyTypeId = table.Column<int>(type: "integer", nullable: false),
                    NumberOfStoreys = table.Column<int>(type: "integer", nullable: false),
                    PriceNegotiableInd = table.Column<bool>(type: "boolean", nullable: false),
                    RentalPropertyTypeId = table.Column<int>(type: "integer", nullable: false),
                    RentalSquareFeet = table.Column<int>(type: "integer", nullable: false),
                    Washrooms = table.Column<int>(type: "integer", nullable: false),
                    SharedWashroomInd = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Baths = table.Column<int>(type: "integer", nullable: false),
                    YearBuilt = table.Column<int>(type: "integer", nullable: false),
                    ParkingSpots = table.Column<int>(type: "integer", nullable: false),
                    RentalParkingSpots = table.Column<int>(type: "integer", nullable: false),
                    ParkingIncludedInd = table.Column<bool>(type: "boolean", nullable: false),
                    PreferredRentalStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PreferredRentalTerm = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    RentalTermNegotiableInd = table.Column<bool>(type: "boolean", nullable: false),
                    FurnishedInd = table.Column<bool>(type: "boolean", nullable: false),
                    WaterIncludedInd = table.Column<bool>(type: "boolean", nullable: false),
                    HeatingIncludedInd = table.Column<bool>(type: "boolean", nullable: false),
                    HydroElectricityIncludedInd = table.Column<bool>(type: "boolean", nullable: false),
                    InternetWiFiIncludedInd = table.Column<bool>(type: "boolean", nullable: false),
                    AdditionalInfo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DeletedInd = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomeRentalDetail", x => x.HomeRentalDetailGUID);
                    table.ForeignKey(
                        name: "FK_HomeRentalDetail_Listing_ListingGUID",
                        column: x => x.ListingGUID,
                        principalTable: "Listing",
                        principalColumn: "ListingGUID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HomeRentalDetail_PropertyType_PropertyTypeId",
                        column: x => x.PropertyTypeId,
                        principalTable: "PropertyType",
                        principalColumn: "PropertyTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HomeRentalDetail_PropertyType_RentalPropertyTypeId",
                        column: x => x.RentalPropertyTypeId,
                        principalTable: "PropertyType",
                        principalColumn: "PropertyTypeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "PropertyType",
                columns: new[] { "PropertyTypeId", "CreatedBy", "CreatedDate", "Description", "ModifiedBy", "ModifiedDate", "Name" },
                values: new object[,]
                {
                    { 1, "system", new DateTime(2026, 2, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Detached or semi-detached house", null, null, "House" },
                    { 2, "system", new DateTime(2026, 2, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Single-storey bungalow", null, null, "Bungalow" },
                    { 3, "system", new DateTime(2026, 2, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Apartment or condominium unit", null, null, "Apartment/Condo" },
                    { 4, "system", new DateTime(2026, 2, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Townhouse", null, null, "Townhouse" },
                    { 5, "system", new DateTime(2026, 2, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Duplex", null, null, "Duplex" },
                    { 6, "system", new DateTime(2026, 2, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Single room", null, null, "Room" },
                    { 7, "system", new DateTime(2026, 2, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Storage space", null, null, "Storage" },
                    { 8, "system", new DateTime(2026, 2, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Dedicated parking spot", null, null, "ParkingSpot" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_HomeRentalDetail_ListingGUID",
                table: "HomeRentalDetail",
                column: "ListingGUID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HomeRentalDetail_PropertyTypeId",
                table: "HomeRentalDetail",
                column: "PropertyTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_HomeRentalDetail_RentalPropertyTypeId",
                table: "HomeRentalDetail",
                column: "RentalPropertyTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyType_Name",
                table: "PropertyType",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HomeRentalDetail");

            migrationBuilder.DropTable(
                name: "PropertyType");
        }
    }
}
