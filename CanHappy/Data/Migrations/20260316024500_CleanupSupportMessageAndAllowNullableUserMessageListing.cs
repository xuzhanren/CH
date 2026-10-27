using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CanHappy.Data.Migrations
{
    /// <inheritdoc />
    public partial class CleanupSupportMessageAndAllowNullableUserMessageListing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserMessage_Listing_ListingGUID",
                table: "UserMessage");

            migrationBuilder.AlterColumn<Guid>(
                name: "ListingGUID",
                table: "UserMessage",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_UserMessage_Listing_ListingGUID",
                table: "UserMessage",
                column: "ListingGUID",
                principalTable: "Listing",
                principalColumn: "ListingGUID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropTable(
                name: "SupportMessage");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SupportMessage",
                columns: table => new
                {
                    SupportMessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    SenderName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    SenderEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Subject = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportMessage", x => x.SupportMessageId);
                    table.ForeignKey(
                        name: "FK_SupportMessage_AspNetUsers_RecipientUserId",
                        column: x => x.RecipientUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupportMessage_RecipientUserId",
                table: "SupportMessage",
                column: "RecipientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportMessage_RecipientUserId_CreatedDate",
                table: "SupportMessage",
                columns: new[] { "RecipientUserId", "CreatedDate" });

            migrationBuilder.DropForeignKey(
                name: "FK_UserMessage_Listing_ListingGUID",
                table: "UserMessage");

            migrationBuilder.Sql("UPDATE \"UserMessage\" SET \"ListingGUID\" = '00000000-0000-0000-0000-000000000000' WHERE \"ListingGUID\" IS NULL;");

            migrationBuilder.AlterColumn<Guid>(
                name: "ListingGUID",
                table: "UserMessage",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UserMessage_Listing_ListingGUID",
                table: "UserMessage",
                column: "ListingGUID",
                principalTable: "Listing",
                principalColumn: "ListingGUID",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
