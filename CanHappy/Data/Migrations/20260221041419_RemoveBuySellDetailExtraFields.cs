using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CanHappy.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBuySellDetailExtraFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Brand",
                table: "BuySellDetail");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "BuySellDetail");

            migrationBuilder.DropColumn(
                name: "Condition",
                table: "BuySellDetail");

            migrationBuilder.DropColumn(
                name: "ManufactureYear",
                table: "BuySellDetail");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "BuySellDetail");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Brand",
                table: "BuySellDetail",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "BuySellDetail",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Condition",
                table: "BuySellDetail",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ManufactureYear",
                table: "BuySellDetail",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "BuySellDetail",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }
    }
}
