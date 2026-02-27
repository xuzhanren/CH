using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CanHappy.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEstateSaleDetailAndEstateRoom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EstateSaleDetail",
                columns: table => new
                {
                    EstateSaleDetailGUID = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingGUID = table.Column<Guid>(type: "uuid", nullable: false),
                    Address = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EstitateTypeId = table.Column<int>(type: "integer", nullable: false),
                    YearBuilt = table.Column<int>(type: "integer", nullable: false),
                    SquareFeet = table.Column<int>(type: "integer", nullable: false),
                    LandSizeSqFt = table.Column<int>(type: "integer", nullable: false),
                    LandDimensionWxD = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    NumberOfStoreys = table.Column<int>(type: "integer", nullable: false),
                    PriceNegotiableInd = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AnuualPropertyTax = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Bedrooms = table.Column<int>(type: "integer", nullable: false),
                    Washrooms = table.Column<int>(type: "integer", nullable: false),
                    Baths = table.Column<int>(type: "integer", nullable: false),
                    ParkingSpots = table.Column<int>(type: "integer", nullable: false),
                    ParkingType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    FoundationType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    HydroType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    WaterType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    SewerType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ExternalStructures = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CoolingType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    HeatingType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    WaterFrontInd = table.Column<bool>(type: "boolean", nullable: false),
                    SoldByOwnerInd = table.Column<bool>(type: "boolean", nullable: false),
                    AppliancesIncluded = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    HasBasementInd = table.Column<bool>(type: "boolean", nullable: false),
                    BasementFinishedInd = table.Column<bool>(type: "boolean", nullable: false),
                    RentalEquipment = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CommunityName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CloseToSchoolInd = table.Column<bool>(type: "boolean", nullable: false),
                    CloseToDaycareInd = table.Column<bool>(type: "boolean", nullable: false),
                    CloseToBusInd = table.Column<bool>(type: "boolean", nullable: false),
                    CloseToShoppingCenterInd = table.Column<bool>(type: "boolean", nullable: false),
                    FurnishedInd = table.Column<bool>(type: "boolean", nullable: false),
                    HasFireplaceInd = table.Column<bool>(type: "boolean", nullable: false),
                    AdditionalInfo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DeletedInd = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstateSaleDetail", x => x.EstateSaleDetailGUID);
                    table.ForeignKey(
                        name: "FK_EstateSaleDetail_EstateType_EstitateTypeId",
                        column: x => x.EstitateTypeId,
                        principalTable: "EstateType",
                        principalColumn: "EstitateTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EstateSaleDetail_Listing_ListingGUID",
                        column: x => x.ListingGUID,
                        principalTable: "Listing",
                        principalColumn: "ListingGUID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EstateRoom",
                columns: table => new
                {
                    EstateRoomGUID = table.Column<Guid>(type: "uuid", nullable: false),
                    EstateSaleDetailGUID = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RoomSizeFtxFt = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    OnFloorNumber = table.Column<int>(type: "integer", nullable: false),
                    ThumbnailURL = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ImageURL = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DeletedInd = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstateRoom", x => x.EstateRoomGUID);
                    table.ForeignKey(
                        name: "FK_EstateRoom_EstateSaleDetail_EstateSaleDetailGUID",
                        column: x => x.EstateSaleDetailGUID,
                        principalTable: "EstateSaleDetail",
                        principalColumn: "EstateSaleDetailGUID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EstateRoom_EstateSaleDetailGUID",
                table: "EstateRoom",
                column: "EstateSaleDetailGUID");

            migrationBuilder.CreateIndex(
                name: "IX_EstateSaleDetail_EstitateTypeId",
                table: "EstateSaleDetail",
                column: "EstitateTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EstateSaleDetail_ListingGUID",
                table: "EstateSaleDetail",
                column: "ListingGUID",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EstateRoom");

            migrationBuilder.DropTable(
                name: "EstateSaleDetail");
        }
    }
}
