using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CanHappy.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessYellowPageDetailEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BusinessYellowPageDetail",
                columns: table => new
                {
                    BusinessYellowPageDetailGUID = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingGUID = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessHours = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BusinessStyle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProductsAndServices = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Specialties = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LanguagesSpoken = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    GeneralBeforeTaxPayPerPerson = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    MethodOfPayments = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    HowToGetThere = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AdditionalInfo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    WebSiteURL = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ActiveInd = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedInd = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessYellowPageDetail", x => x.BusinessYellowPageDetailGUID);
                    table.ForeignKey(
                        name: "FK_BusinessYellowPageDetail_Listing_ListingGUID",
                        column: x => x.ListingGUID,
                        principalTable: "Listing",
                        principalColumn: "ListingGUID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessYellowPageDetail_ListingGUID",
                table: "BusinessYellowPageDetail",
                column: "ListingGUID",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessYellowPageDetail");
        }
    }
}
