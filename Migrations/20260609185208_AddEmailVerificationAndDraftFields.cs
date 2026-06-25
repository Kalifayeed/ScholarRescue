using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ScholarRescue.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailVerificationAndDraftFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DraftSavedAt",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDraft",
                table: "Orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerifiedAt",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RegistrationCompleted",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationSource",
                table: "AspNetUsers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AssignmentHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    RecommendedWriterIds = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    AssignedWriterId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    WasAutoAssigned = table.Column<bool>(type: "boolean", nullable: false),
                    AssignedWriterMatchScore = table.Column<double>(type: "double precision", nullable: false),
                    WasCompletedSuccessfully = table.Column<bool>(type: "boolean", nullable: true),
                    ClientRating = table.Column<double>(type: "double precision", nullable: true),
                    WasOnTime = table.Column<bool>(type: "boolean", nullable: true),
                    RevisionCount = table.Column<int>(type: "integer", nullable: false),
                    HadDispute = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssignmentHistories_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WriterMatchScores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    WriterId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    MatchPercentage = table.Column<double>(type: "double precision", nullable: false),
                    ExpertiseScore = table.Column<double>(type: "double precision", nullable: false),
                    AcademicLevelScore = table.Column<double>(type: "double precision", nullable: false),
                    ReliabilityScore = table.Column<double>(type: "double precision", nullable: false),
                    RatingScore = table.Column<double>(type: "double precision", nullable: false),
                    CapacityScore = table.Column<double>(type: "double precision", nullable: false),
                    DeadlineScore = table.Column<double>(type: "double precision", nullable: false),
                    PerformanceScore = table.Column<double>(type: "double precision", nullable: false),
                    QualityScore = table.Column<double>(type: "double precision", nullable: false),
                    TotalScore = table.Column<double>(type: "double precision", nullable: false),
                    Explanation = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WriterMatchScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WriterMatchScores_AspNetUsers_WriterId",
                        column: x => x.WriterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WriterMatchScores_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_AssignedWriterId_Status",
                table: "Orders",
                columns: new[] { "AssignedWriterId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status_CreatedAt",
                table: "Orders",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderApplications_OrderId_Status",
                table: "OrderApplications",
                columns: new[] { "OrderId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead_CreatedAt",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Action_CreatedDate",
                table: "AuditLogs",
                columns: new[] { "Action", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentHistory_OrderId",
                table: "AssignmentHistories",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_WriterMatchScores_OrderId_MatchPercentage",
                table: "WriterMatchScores",
                columns: new[] { "OrderId", "MatchPercentage" });

            migrationBuilder.CreateIndex(
                name: "IX_WriterMatchScores_WriterId",
                table: "WriterMatchScores",
                column: "WriterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssignmentHistories");

            migrationBuilder.DropTable(
                name: "WriterMatchScores");

            migrationBuilder.DropIndex(
                name: "IX_Orders_AssignedWriterId_Status",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Status_CreatedAt",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_OrderApplications_OrderId_Status",
                table: "OrderApplications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_IsRead_CreatedAt",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_Action_CreatedDate",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "DraftSavedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IsDraft",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "EmailVerifiedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RegistrationCompleted",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RegistrationSource",
                table: "AspNetUsers");
        }
    }
}
