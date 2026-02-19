using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CanHappy.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddListingPriceAndDiscountFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DiscountBeginDate",
                table: "Listing",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DiscountEndDate",
                table: "Listing",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercent",
                table: "Listing",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0.00m);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Listing",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceMax",
                table: "Listing",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceMin",
                table: "Listing",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountBeginDate",
                table: "Listing");

            migrationBuilder.DropColumn(
                name: "DiscountEndDate",
                table: "Listing");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "Listing");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Listing");

            migrationBuilder.DropColumn(
                name: "PriceMax",
                table: "Listing");

            migrationBuilder.DropColumn(
                name: "PriceMin",
                table: "Listing");
        }
    }
}
