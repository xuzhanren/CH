using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CanHappy.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdCategorySubcategoryProvinceAndStatusFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ActiveInd",
                table: "Ad",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Ad",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClickCount",
                table: "Ad",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "PaidInd",
                table: "Ad",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ProvinceId",
                table: "Ad",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubcategoryId",
                table: "Ad",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ad_CategoryId",
                table: "Ad",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Ad_ProvinceId",
                table: "Ad",
                column: "ProvinceId");

            migrationBuilder.CreateIndex(
                name: "IX_Ad_SubcategoryId",
                table: "Ad",
                column: "SubcategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Ad_Category_CategoryId",
                table: "Ad",
                column: "CategoryId",
                principalTable: "Category",
                principalColumn: "CategoryId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Ad_Province_ProvinceId",
                table: "Ad",
                column: "ProvinceId",
                principalTable: "Province",
                principalColumn: "ProvinceId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Ad_Subcategory_SubcategoryId",
                table: "Ad",
                column: "SubcategoryId",
                principalTable: "Subcategory",
                principalColumn: "SubcategoryId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ad_Category_CategoryId",
                table: "Ad");

            migrationBuilder.DropForeignKey(
                name: "FK_Ad_Province_ProvinceId",
                table: "Ad");

            migrationBuilder.DropForeignKey(
                name: "FK_Ad_Subcategory_SubcategoryId",
                table: "Ad");

            migrationBuilder.DropIndex(
                name: "IX_Ad_CategoryId",
                table: "Ad");

            migrationBuilder.DropIndex(
                name: "IX_Ad_ProvinceId",
                table: "Ad");

            migrationBuilder.DropIndex(
                name: "IX_Ad_SubcategoryId",
                table: "Ad");

            migrationBuilder.DropColumn(
                name: "ActiveInd",
                table: "Ad");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Ad");

            migrationBuilder.DropColumn(
                name: "ClickCount",
                table: "Ad");

            migrationBuilder.DropColumn(
                name: "PaidInd",
                table: "Ad");

            migrationBuilder.DropColumn(
                name: "ProvinceId",
                table: "Ad");

            migrationBuilder.DropColumn(
                name: "SubcategoryId",
                table: "Ad");
        }
    }
}
