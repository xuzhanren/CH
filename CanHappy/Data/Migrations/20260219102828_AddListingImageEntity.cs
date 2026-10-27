using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CanHappy.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddListingImageEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ListingImage",
                columns: table => new
                {
                    ListingImageGUID = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingGUID = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SorOrder = table.Column<int>(type: "integer", nullable: false),
                    ThumbnailURL = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ImageURL = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DeletedInd = table.Column<bool>(type: "boolean", nullable: false),
                    SampleInd = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListingImage", x => x.ListingImageGUID);
                    table.ForeignKey(
                        name: "FK_ListingImage_Listing_ListingGUID",
                        column: x => x.ListingGUID,
                        principalTable: "Listing",
                        principalColumn: "ListingGUID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ListingImage_ListingGUID",
                table: "ListingImage",
                column: "ListingGUID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ListingImage");
        }
    }
}
