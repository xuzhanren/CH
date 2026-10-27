using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CanHappy.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddListingConditionBrandQuantityYear : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Brand",
                table: "Listing",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Condition",
                table: "Listing",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "Used");

            migrationBuilder.AddColumn<int>(
                name: "ManufactureYear",
                table: "Listing",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Model",
                table: "Listing",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "Listing",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Brand",
                table: "Listing");

            migrationBuilder.DropColumn(
                name: "Condition",
                table: "Listing");

            migrationBuilder.DropColumn(
                name: "ManufactureYear",
                table: "Listing");

            migrationBuilder.DropColumn(
                name: "Model",
                table: "Listing");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "Listing");
        }
    }
}
