using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarRescue.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedbackQualityChecklistToQaReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminNotes",
                table: "QaReviews",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FeedbackIsActionable",
                table: "QaReviews",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FeedbackIsSubstantive",
                table: "QaReviews",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FeedbackNotesQualityPassed",
                table: "QaReviews",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MatchesRequestType",
                table: "QaReviews",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PreservesStudentVoice",
                table: "QaReviews",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminNotes",
                table: "QaReviews");

            migrationBuilder.DropColumn(
                name: "FeedbackIsActionable",
                table: "QaReviews");

            migrationBuilder.DropColumn(
                name: "FeedbackIsSubstantive",
                table: "QaReviews");

            migrationBuilder.DropColumn(
                name: "FeedbackNotesQualityPassed",
                table: "QaReviews");

            migrationBuilder.DropColumn(
                name: "MatchesRequestType",
                table: "QaReviews");

            migrationBuilder.DropColumn(
                name: "PreservesStudentVoice",
                table: "QaReviews");
        }
    }
}
