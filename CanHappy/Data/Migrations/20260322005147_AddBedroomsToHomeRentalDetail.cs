using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CanHappy.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBedroomsToHomeRentalDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Bedrooms",
                table: "HomeRentalDetail",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BedroomsForRental",
                table: "HomeRentalDetail",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Bedrooms",
                table: "HomeRentalDetail");

            migrationBuilder.DropColumn(
                name: "BedroomsForRental",
                table: "HomeRentalDetail");
        }
    }
}
