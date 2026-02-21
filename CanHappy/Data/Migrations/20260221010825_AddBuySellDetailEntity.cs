using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CanHappy.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBuySellDetailEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BuySellDetail",
                columns: table => new
                {
                    BuySellDetailGUID = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingGUID = table.Column<Guid>(type: "uuid", nullable: false),
                    Brand = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Model = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Condition = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ItemLocation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Material = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    ManufactureYear = table.Column<int>(type: "integer", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    NegotiablePriceInd = table.Column<bool>(type: "boolean", nullable: false),
                    DeliveryAvailableInd = table.Column<bool>(type: "boolean", nullable: false),
                    PickupAvailableInd = table.Column<bool>(type: "boolean", nullable: false),
                    WarrantyInfo = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    AdditionalDetails = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DeletedInd = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ModifiedBY = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuySellDetail", x => x.BuySellDetailGUID);
                    table.ForeignKey(
                        name: "FK_BuySellDetail_Listing_ListingGUID",
                        column: x => x.ListingGUID,
                        principalTable: "Listing",
                        principalColumn: "ListingGUID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BuySellDetail_ListingGUID",
                table: "BuySellDetail",
                column: "ListingGUID",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BuySellDetail");
        }
    }
}
