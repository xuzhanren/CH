using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CanHappy.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesSpecialsImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SalesSpecialsImage",
                columns: table => new
                {
                    SalesSpecialsImageGUID = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingGUID = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Price = table.Column<decimal>(type: "money", nullable: true),
                    SalePrice = table.Column<decimal>(type: "money", nullable: true),
                    PercentOff = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true),
                    SaleBegin = table.Column<DateTime>(type: "date", nullable: true),
                    SaleEnd = table.Column<DateTime>(type: "date", nullable: true),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ThumbnailURL = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    ImageURL = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    DeletedInd = table.Column<bool>(type: "boolean", nullable: false),
                    SampleInd = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "date", nullable: false, defaultValueSql: "CURRENT_DATE"),
                    ModifiedBy = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesSpecialsImage", x => x.SalesSpecialsImageGUID);
                    table.ForeignKey(
                        name: "FK_SalesSpecialsImage_Listing_ListingGUID",
                        column: x => x.ListingGUID,
                        principalTable: "Listing",
                        principalColumn: "ListingGUID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesSpecialsImage_ListingGUID",
                table: "SalesSpecialsImage",
                column: "ListingGUID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalesSpecialsImage");
        }
    }
}
