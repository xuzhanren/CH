using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CanHappy.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddThumbnailUrlToListing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ThumbnailURL",
                table: "Listing",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ThumbnailURL",
                table: "Listing");
        }
    }
}
