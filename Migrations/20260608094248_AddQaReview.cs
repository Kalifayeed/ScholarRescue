using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ScholarRescue.Migrations
{
    /// <inheritdoc />
    public partial class AddQaReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QaReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    SubmissionId = table.Column<int>(type: "integer", nullable: false),
                    ReviewerId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    FormattingPassed = table.Column<bool>(type: "boolean", nullable: false),
                    WordCountPassed = table.Column<bool>(type: "boolean", nullable: false),
                    FilesPassed = table.Column<bool>(type: "boolean", nullable: false),
                    InstructionsPassed = table.Column<bool>(type: "boolean", nullable: false),
                    PlagiarismReportPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QaReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QaReviews_AspNetUsers_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QaReviews_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QaReviews_OrderId",
                table: "QaReviews",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_QaReviews_ReviewerId",
                table: "QaReviews",
                column: "ReviewerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QaReviews");
        }
    }
}
