using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ScholarRescue.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WriterApplications_AspNetUsers_ReviewedById",
                table: "WriterApplications");

            migrationBuilder.DropIndex(
                name: "IX_WriterApplications_ReviewedById",
                table: "WriterApplications");

            migrationBuilder.DropIndex(
                name: "IX_WriterApplications_SubmittedDate",
                table: "WriterApplications");

            migrationBuilder.RenameColumn(
                name: "Specializations",
                table: "WriterApplications",
                newName: "Specialization");

            migrationBuilder.AlterColumn<string>(
                name: "ReviewNotes",
                table: "WriterApplications",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Institution",
                table: "WriterApplications",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300);

            migrationBuilder.AlterColumn<string>(
                name: "EducationLevel",
                table: "WriterApplications",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "AdminComments",
                table: "WriterApplications",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Biography",
                table: "WriterApplications",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CvFilePath",
                table: "WriterApplications",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DegreeFilePath",
                table: "WriterApplications",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HighestQualification",
                table: "WriterApplications",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "WriterApplications",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "WriterApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewedByAdminId",
                table: "WriterApplications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "WriterApplications",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "WritingSampleFilePath",
                table: "WriterApplications",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedAt",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignedByAdminId",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDisputed",
                table: "Orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsMarketplaceOpen",
                table: "Orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "RatedAt",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Rating",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AvailabilityStatus",
                table: "AspNetUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastActivityDate",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrderApplications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    WriterId = table.Column<string>(type: "text", nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProcessedByAdminId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderApplications_AspNetUsers_ProcessedByAdminId",
                        column: x => x.ProcessedByAdminId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OrderApplications_AspNetUsers_WriterId",
                        column: x => x.WriterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderApplications_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderMilestones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Pages = table.Column<int>(type: "integer", nullable: false),
                    Deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedById = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    ApprovalNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SubmissionNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ApprovedEarnings = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    LedgerTransactionNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderMilestones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderMilestones_AspNetUsers_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OrderMilestones_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderSubmissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    WriterId = table.Column<string>(type: "text", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    SubmissionType = table.Column<int>(type: "integer", nullable: false),
                    FilePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Comments = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderSubmissions_AspNetUsers_WriterId",
                        column: x => x.WriterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderSubmissions_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RevisionRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    ClientId = table.Column<string>(type: "text", nullable: false),
                    WriterId = table.Column<string>(type: "text", nullable: false),
                    Comments = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevisionRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RevisionRequests_AspNetUsers_ClientId",
                        column: x => x.ClientId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RevisionRequests_AspNetUsers_WriterId",
                        column: x => x.WriterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RevisionRequests_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupportTickets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TicketNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Department = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatorId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    AssignedAdminId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    OrderId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportTickets_AspNetUsers_AssignedAdminId",
                        column: x => x.AssignedAdminId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SupportTickets_AspNetUsers_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupportTickets_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "WriterRankings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WriterId = table.Column<string>(type: "text", nullable: false),
                    CurrentRank = table.Column<int>(type: "integer", nullable: false),
                    IsOverridden = table.Column<bool>(type: "boolean", nullable: false),
                    OverrideAdminId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    OverriddenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OverrideNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CompletedOrders = table.Column<int>(type: "integer", nullable: false),
                    TotalRating = table.Column<int>(type: "integer", nullable: false),
                    TotalRatings = table.Column<int>(type: "integer", nullable: false),
                    OrdersWithRevisions = table.Column<int>(type: "integer", nullable: false),
                    DisputedOrders = table.Column<int>(type: "integer", nullable: false),
                    OnTimeDeliveries = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WriterRankings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WriterRankings_AspNetUsers_WriterId",
                        column: x => x.WriterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WriterResources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    SubCategory = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Tags = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Question = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AuthorId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WriterResources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderMilestoneFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MilestoneId = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderMilestoneFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderMilestoneFiles_OrderMilestones_MilestoneId",
                        column: x => x.MilestoneId,
                        principalTable: "OrderMilestones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupportTicketAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TicketId = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UploadedById = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTicketAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportTicketAttachments_SupportTickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "SupportTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupportTicketNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TicketId = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    AuthorId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    IsInternal = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTicketNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportTicketNotes_AspNetUsers_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupportTicketNotes_SupportTickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "SupportTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WriterApplications_ReviewedByAdminId",
                table: "WriterApplications",
                column: "ReviewedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_WriterApplications_SubmittedAt",
                table: "WriterApplications",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_AssignedByAdminId",
                table: "Orders",
                column: "AssignedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_IsMarketplaceOpen",
                table: "Orders",
                column: "IsMarketplaceOpen");

            migrationBuilder.CreateIndex(
                name: "IX_OrderApplications_AppliedAt",
                table: "OrderApplications",
                column: "AppliedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OrderApplications_OrderId_WriterId",
                table: "OrderApplications",
                columns: new[] { "OrderId", "WriterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderApplications_ProcessedByAdminId",
                table: "OrderApplications",
                column: "ProcessedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderApplications_Status",
                table: "OrderApplications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OrderApplications_WriterId",
                table: "OrderApplications",
                column: "WriterId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderMilestoneFiles_MilestoneId",
                table: "OrderMilestoneFiles",
                column: "MilestoneId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderMilestones_ApprovedById",
                table: "OrderMilestones",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_OrderMilestones_OrderId",
                table: "OrderMilestones",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderMilestones_OrderId_SortOrder",
                table: "OrderMilestones",
                columns: new[] { "OrderId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderMilestones_Status",
                table: "OrderMilestones",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OrderSubmissions_OrderId",
                table: "OrderSubmissions",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderSubmissions_WriterId",
                table: "OrderSubmissions",
                column: "WriterId");

            migrationBuilder.CreateIndex(
                name: "IX_RevisionRequests_ClientId",
                table: "RevisionRequests",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_RevisionRequests_OrderId",
                table: "RevisionRequests",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_RevisionRequests_WriterId",
                table: "RevisionRequests",
                column: "WriterId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicketAttachments_TicketId",
                table: "SupportTicketAttachments",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicketNotes_AuthorId",
                table: "SupportTicketNotes",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicketNotes_TicketId",
                table: "SupportTicketNotes",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_AssignedAdminId",
                table: "SupportTickets",
                column: "AssignedAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_CreatedAt",
                table: "SupportTickets",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_CreatorId",
                table: "SupportTickets",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_Department",
                table: "SupportTickets",
                column: "Department");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_OrderId",
                table: "SupportTickets",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_Status",
                table: "SupportTickets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_TicketNumber",
                table: "SupportTickets",
                column: "TicketNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WriterRankings_CurrentRank",
                table: "WriterRankings",
                column: "CurrentRank");

            migrationBuilder.CreateIndex(
                name: "IX_WriterRankings_IsOverridden",
                table: "WriterRankings",
                column: "IsOverridden");

            migrationBuilder.CreateIndex(
                name: "IX_WriterRankings_WriterId",
                table: "WriterRankings",
                column: "WriterId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WriterResources_Category",
                table: "WriterResources",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_WriterResources_Category_SubCategory",
                table: "WriterResources",
                columns: new[] { "Category", "SubCategory" });

            migrationBuilder.CreateIndex(
                name: "IX_WriterResources_IsActive",
                table: "WriterResources",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_WriterResources_SortOrder",
                table: "WriterResources",
                column: "SortOrder");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_AspNetUsers_AssignedByAdminId",
                table: "Orders",
                column: "AssignedByAdminId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_WriterApplications_AspNetUsers_ReviewedByAdminId",
                table: "WriterApplications",
                column: "ReviewedByAdminId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_AspNetUsers_AssignedByAdminId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_WriterApplications_AspNetUsers_ReviewedByAdminId",
                table: "WriterApplications");

            migrationBuilder.DropTable(
                name: "OrderApplications");

            migrationBuilder.DropTable(
                name: "OrderMilestoneFiles");

            migrationBuilder.DropTable(
                name: "OrderSubmissions");

            migrationBuilder.DropTable(
                name: "RevisionRequests");

            migrationBuilder.DropTable(
                name: "SupportTicketAttachments");

            migrationBuilder.DropTable(
                name: "SupportTicketNotes");

            migrationBuilder.DropTable(
                name: "WriterRankings");

            migrationBuilder.DropTable(
                name: "WriterResources");

            migrationBuilder.DropTable(
                name: "OrderMilestones");

            migrationBuilder.DropTable(
                name: "SupportTickets");

            migrationBuilder.DropIndex(
                name: "IX_WriterApplications_ReviewedByAdminId",
                table: "WriterApplications");

            migrationBuilder.DropIndex(
                name: "IX_WriterApplications_SubmittedAt",
                table: "WriterApplications");

            migrationBuilder.DropIndex(
                name: "IX_Orders_AssignedByAdminId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_IsMarketplaceOpen",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "AdminComments",
                table: "WriterApplications");

            migrationBuilder.DropColumn(
                name: "Biography",
                table: "WriterApplications");

            migrationBuilder.DropColumn(
                name: "CvFilePath",
                table: "WriterApplications");

            migrationBuilder.DropColumn(
                name: "DegreeFilePath",
                table: "WriterApplications");

            migrationBuilder.DropColumn(
                name: "HighestQualification",
                table: "WriterApplications");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "WriterApplications");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "WriterApplications");

            migrationBuilder.DropColumn(
                name: "ReviewedByAdminId",
                table: "WriterApplications");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "WriterApplications");

            migrationBuilder.DropColumn(
                name: "WritingSampleFilePath",
                table: "WriterApplications");

            migrationBuilder.DropColumn(
                name: "AssignedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "AssignedByAdminId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IsDisputed",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IsMarketplaceOpen",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RatedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "AvailabilityStatus",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastActivityDate",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "Specialization",
                table: "WriterApplications",
                newName: "Specializations");

            migrationBuilder.AlterColumn<string>(
                name: "ReviewNotes",
                table: "WriterApplications",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Institution",
                table: "WriterApplications",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EducationLevel",
                table: "WriterApplications",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WriterApplications_ReviewedById",
                table: "WriterApplications",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_WriterApplications_SubmittedDate",
                table: "WriterApplications",
                column: "SubmittedDate");

            migrationBuilder.AddForeignKey(
                name: "FK_WriterApplications_AspNetUsers_ReviewedById",
                table: "WriterApplications",
                column: "ReviewedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
