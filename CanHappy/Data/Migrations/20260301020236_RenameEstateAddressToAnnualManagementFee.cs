using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CanHappy.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameEstateAddressToAnnualManagementFee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "EstateSaleDetail");

            migrationBuilder.AddColumn<decimal>(
                name: "AnnualManagementFee",
                table: "EstateSaleDetail",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnnualManagementFee",
                table: "EstateSaleDetail");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "EstateSaleDetail",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}
