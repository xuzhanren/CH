using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CanHappy.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCarPoolAndRideRequestEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CarPoolStatus",
                columns: table => new
                {
                    CarPoolStatusId = table.Column<int>(type: "integer", nullable: false)
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
                    table.PrimaryKey("PK_CarPoolStatus", x => x.CarPoolStatusId);
                });

            migrationBuilder.CreateTable(
                name: "CarPoolType",
                columns: table => new
                {
                    CarPoolTypeId = table.Column<int>(type: "integer", nullable: false)
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
                    table.PrimaryKey("PK_CarPoolType", x => x.CarPoolTypeId);
                });

            migrationBuilder.CreateTable(
                name: "RideRequestStatus",
                columns: table => new
                {
                    RideRequestStatusId = table.Column<int>(type: "integer", nullable: false)
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
                    table.PrimaryKey("PK_RideRequestStatus", x => x.RideRequestStatusId);
                });

            migrationBuilder.CreateTable(
                name: "CarPoolDetail",
                columns: table => new
                {
                    CarPoolDetailGUID = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingGUID = table.Column<Guid>(type: "uuid", nullable: false),
                    CarPoolTypeId = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CarPoolStatusId = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    FromCity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LeavingDate = table.Column<DateTime>(type: "date", nullable: true),
                    WeekDays = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LeavingTime = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PickupLocation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DestinationCity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DropoffLocation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TripStops = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    VehicleModelYear = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    VehicleLicensePlateNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    VehicleColor = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    WeeklyScheduleInd = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    SmallBagAllowedInd = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    MediumBagAllowedInd = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    OneLargeBagAllowedInd = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AdditionalInfo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DeletedInd = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarPoolDetail", x => x.CarPoolDetailGUID);
                    table.ForeignKey(
                        name: "FK_CarPoolDetail_CarPoolStatus_CarPoolStatusId",
                        column: x => x.CarPoolStatusId,
                        principalTable: "CarPoolStatus",
                        principalColumn: "CarPoolStatusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CarPoolDetail_CarPoolType_CarPoolTypeId",
                        column: x => x.CarPoolTypeId,
                        principalTable: "CarPoolType",
                        principalColumn: "CarPoolTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CarPoolDetail_Listing_ListingGUID",
                        column: x => x.ListingGUID,
                        principalTable: "Listing",
                        principalColumn: "ListingGUID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RideRequest",
                columns: table => new
                {
                    RideRequestGUID = table.Column<Guid>(type: "uuid", nullable: false),
                    CarPoolDetailGUID = table.Column<Guid>(type: "uuid", nullable: false),
                    RideRequestStatusId = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    RiderUserID = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestMessage = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DeletedInd = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RideRequest", x => x.RideRequestGUID);
                    table.ForeignKey(
                        name: "FK_RideRequest_CarPoolDetail_CarPoolDetailGUID",
                        column: x => x.CarPoolDetailGUID,
                        principalTable: "CarPoolDetail",
                        principalColumn: "CarPoolDetailGUID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RideRequest_RideRequestStatus_RideRequestStatusId",
                        column: x => x.RideRequestStatusId,
                        principalTable: "RideRequestStatus",
                        principalColumn: "RideRequestStatusId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "CarPoolStatus",
                columns: new[] { "CarPoolStatusId", "CreatedBy", "CreatedDate", "Description", "ModifiedBy", "ModifiedDate", "Name" },
                values: new object[,]
                {
                    { 1, "system", new DateTime(2026, 3, 7, 0, 0, 0, 0, DateTimeKind.Utc), "Car pool entry is released.", null, null, "Released" },
                    { 2, "system", new DateTime(2026, 3, 7, 0, 0, 0, 0, DateTimeKind.Utc), "Car pool entry is on hold.", null, null, "On Hold" },
                    { 3, "system", new DateTime(2026, 3, 7, 0, 0, 0, 0, DateTimeKind.Utc), "Car pool entry is cancelled.", null, null, "Cancelled" },
                    { 4, "system", new DateTime(2026, 3, 7, 0, 0, 0, 0, DateTimeKind.Utc), "Car pool entry is suspended by clerk.", null, null, "Suspended by Clerk" }
                });

            migrationBuilder.InsertData(
                table: "CarPoolType",
                columns: new[] { "CarPoolTypeId", "CreatedBy", "CreatedDate", "Description", "ModifiedBy", "ModifiedDate", "Name" },
                values: new object[,]
                {
                    { 1, "system", new DateTime(2026, 3, 7, 0, 0, 0, 0, DateTimeKind.Utc), "Driver offers a ride.", null, null, "Offer a Ride" },
                    { 2, "system", new DateTime(2026, 3, 7, 0, 0, 0, 0, DateTimeKind.Utc), "Rider needs a ride.", null, null, "Need a Ride" }
                });

            migrationBuilder.InsertData(
                table: "RideRequestStatus",
                columns: new[] { "RideRequestStatusId", "CreatedBy", "CreatedDate", "Description", "ModifiedBy", "ModifiedDate", "Name" },
                values: new object[,]
                {
                    { 1, "system", new DateTime(2026, 3, 7, 0, 0, 0, 0, DateTimeKind.Utc), "Ride requested by rider.", null, null, "Ride Requested by Rider" },
                    { 2, "system", new DateTime(2026, 3, 7, 0, 0, 0, 0, DateTimeKind.Utc), "Ride accepted by driver.", null, null, "Ride Accepted by Driver" },
                    { 3, "system", new DateTime(2026, 3, 7, 0, 0, 0, 0, DateTimeKind.Utc), "Ride cancelled by rider.", null, null, "Ride Cancelled by Rider" },
                    { 4, "system", new DateTime(2026, 3, 7, 0, 0, 0, 0, DateTimeKind.Utc), "Ride cancelled by driver.", null, null, "Ride Cancelled by Driver" },
                    { 5, "system", new DateTime(2026, 3, 7, 0, 0, 0, 0, DateTimeKind.Utc), "Ride cancelled by clerk.", null, null, "Ride Cancelled by Clerk" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CarPoolDetail_CarPoolStatusId",
                table: "CarPoolDetail",
                column: "CarPoolStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_CarPoolDetail_CarPoolTypeId",
                table: "CarPoolDetail",
                column: "CarPoolTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_CarPoolDetail_ListingGUID",
                table: "CarPoolDetail",
                column: "ListingGUID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CarPoolStatus_Name",
                table: "CarPoolStatus",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CarPoolType_Name",
                table: "CarPoolType",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RideRequest_CarPoolDetailGUID",
                table: "RideRequest",
                column: "CarPoolDetailGUID");

            migrationBuilder.CreateIndex(
                name: "IX_RideRequest_RideRequestStatusId",
                table: "RideRequest",
                column: "RideRequestStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_RideRequest_RiderUserID",
                table: "RideRequest",
                column: "RiderUserID");

            migrationBuilder.CreateIndex(
                name: "IX_RideRequest_RiderUserID_DeletedInd_CreatedDate",
                table: "RideRequest",
                columns: new[] { "RiderUserID", "DeletedInd", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_RideRequestStatus_Name",
                table: "RideRequestStatus",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RideRequest");

            migrationBuilder.DropTable(
                name: "CarPoolDetail");

            migrationBuilder.DropTable(
                name: "RideRequestStatus");

            migrationBuilder.DropTable(
                name: "CarPoolStatus");

            migrationBuilder.DropTable(
                name: "CarPoolType");
        }
    }
}
