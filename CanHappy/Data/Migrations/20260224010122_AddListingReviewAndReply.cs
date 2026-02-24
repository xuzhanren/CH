using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CanHappy.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddListingReviewAndReply : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Rating",
                table: "Listing",
                type: "character varying(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "3.5");

            migrationBuilder.CreateTable(
                name: "ListingReview",
                columns: table => new
                {
                    ListingReviewGUID = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingGUID = table.Column<Guid>(type: "uuid", nullable: false),
                    Rating = table.Column<float>(type: "real", precision: 2, scale: 1, nullable: false),
                    ReviewTitle = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ReviewMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Submitted"),
                    VerifiedPurchaseInd = table.Column<bool>(type: "boolean", nullable: false),
                    HelpfulCount = table.Column<int>(type: "integer", nullable: false),
                    ReportedCount = table.Column<int>(type: "integer", nullable: false),
                    DeletedInd = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListingReview", x => x.ListingReviewGUID);
                    table.ForeignKey(
                        name: "FK_ListingReview_Listing_ListingGUID",
                        column: x => x.ListingGUID,
                        principalTable: "Listing",
                        principalColumn: "ListingGUID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ListingReviewImage",
                columns: table => new
                {
                    ListingReviewImageGUID = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingReviewGUID = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    ThumbnailURL = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ImageURL = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DeletedInd = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListingReviewImage", x => x.ListingReviewImageGUID);
                    table.ForeignKey(
                        name: "FK_ListingReviewImage_ListingReview_ListingReviewGUID",
                        column: x => x.ListingReviewGUID,
                        principalTable: "ListingReview",
                        principalColumn: "ListingReviewGUID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReviewReply",
                columns: table => new
                {
                    ReviewReplyGUID = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingReviewGUID = table.Column<Guid>(type: "uuid", nullable: false),
                    ReplyMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Submitted"),
                    OfficialReplyInd = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedInd = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewReply", x => x.ReviewReplyGUID);
                    table.ForeignKey(
                        name: "FK_ReviewReply_ListingReview_ListingReviewGUID",
                        column: x => x.ListingReviewGUID,
                        principalTable: "ListingReview",
                        principalColumn: "ListingReviewGUID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ListingReview_ListingGUID",
                table: "ListingReview",
                column: "ListingGUID");

            migrationBuilder.CreateIndex(
                name: "IX_ListingReview_ListingGUID_DeletedInd_CreatedDate",
                table: "ListingReview",
                columns: new[] { "ListingGUID", "DeletedInd", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ListingReviewImage_ListingReviewGUID",
                table: "ListingReviewImage",
                column: "ListingReviewGUID");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewReply_ListingReviewGUID",
                table: "ReviewReply",
                column: "ListingReviewGUID");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewReply_ListingReviewGUID_DeletedInd_CreatedDate",
                table: "ReviewReply",
                columns: new[] { "ListingReviewGUID", "DeletedInd", "CreatedDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ListingReviewImage");

            migrationBuilder.DropTable(
                name: "ReviewReply");

            migrationBuilder.DropTable(
                name: "ListingReview");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Listing");
        }
    }
}
