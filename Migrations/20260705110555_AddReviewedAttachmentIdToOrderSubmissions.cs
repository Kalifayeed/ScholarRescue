using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarRescue.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewedAttachmentIdToOrderSubmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReviewedAttachmentId",
                table: "OrderSubmissions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderSubmissions_ReviewedAttachmentId",
                table: "OrderSubmissions",
                column: "ReviewedAttachmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderSubmissions_OrderAttachments_ReviewedAttachmentId",
                table: "OrderSubmissions",
                column: "ReviewedAttachmentId",
                principalTable: "OrderAttachments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderSubmissions_OrderAttachments_ReviewedAttachmentId",
                table: "OrderSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_OrderSubmissions_ReviewedAttachmentId",
                table: "OrderSubmissions");

            migrationBuilder.DropColumn(
                name: "ReviewedAttachmentId",
                table: "OrderSubmissions");
        }
    }
}
