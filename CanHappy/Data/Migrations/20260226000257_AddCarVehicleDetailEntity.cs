using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CanHappy.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCarVehicleDetailEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CarVehicleDetail",
                columns: table => new
                {
                    CarVehicleDetailGUID = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingGUID = table.Column<Guid>(type: "uuid", nullable: false),
                    Kilometers = table.Column<int>(type: "integer", nullable: false),
                    Doors = table.Column<int>(type: "integer", nullable: false),
                    Seats = table.Column<int>(type: "integer", nullable: false),
                    BodyStyle = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Engine = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ExteriorColor = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    InteriorColor = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Transmission = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Drivetrain = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    FuelType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    SellerType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    LeatherSeatsInd = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    BackupCameraInd = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AlloyWheelsInd = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    BluetoothInd = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    HeatedSeatsInd = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CarPlayInd = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AndroidAutoInd = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    NavigationMapInd = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RemoteStartInd = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    SunroofInd = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    MoonroofInd = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    BlindSpotMonitoringInd = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    LaneTrackingInd = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AdaptiveCruiseInd = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AssistedParkingCameraInd = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedInd = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarVehicleDetail", x => x.CarVehicleDetailGUID);
                    table.ForeignKey(
                        name: "FK_CarVehicleDetail_Listing_ListingGUID",
                        column: x => x.ListingGUID,
                        principalTable: "Listing",
                        principalColumn: "ListingGUID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CarVehicleDetail_ListingGUID",
                table: "CarVehicleDetail",
                column: "ListingGUID",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CarVehicleDetail");
        }
    }
}
