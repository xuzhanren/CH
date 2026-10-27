using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CanHappy.Data.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeAdStatusAndSizeForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdSize",
                table: "Ad");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Ad");

            migrationBuilder.AddColumn<int>(
                name: "AdSizeId",
                table: "Ad",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AdStatusId",
                table: "Ad",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Ad_AdSizeId",
                table: "Ad",
                column: "AdSizeId");

            migrationBuilder.CreateIndex(
                name: "IX_Ad_AdStatusId",
                table: "Ad",
                column: "AdStatusId");

            migrationBuilder.AddForeignKey(
                name: "FK_Ad_AdSize_AdSizeId",
                table: "Ad",
                column: "AdSizeId",
                principalTable: "AdSize",
                principalColumn: "AdSizeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Ad_AdStatus_AdStatusId",
                table: "Ad",
                column: "AdStatusId",
                principalTable: "AdStatus",
                principalColumn: "AdStatusId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ad_AdSize_AdSizeId",
                table: "Ad");

            migrationBuilder.DropForeignKey(
                name: "FK_Ad_AdStatus_AdStatusId",
                table: "Ad");

            migrationBuilder.DropIndex(
                name: "IX_Ad_AdSizeId",
                table: "Ad");

            migrationBuilder.DropIndex(
                name: "IX_Ad_AdStatusId",
                table: "Ad");

            migrationBuilder.DropColumn(
                name: "AdSizeId",
                table: "Ad");

            migrationBuilder.DropColumn(
                name: "AdStatusId",
                table: "Ad");

            migrationBuilder.AddColumn<string>(
                name: "AdSize",
                table: "Ad",
                type: "character varying(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Ad",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Draft");
        }
    }
}
