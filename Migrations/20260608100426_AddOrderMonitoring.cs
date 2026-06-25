using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ScholarRescue.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderMonitoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MonitoringAlerts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AlertType = table.Column<int>(type: "integer", nullable: false),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    WriterId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    MilestoneId = table.Column<int>(type: "integer", nullable: true),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IsAcknowledged = table.Column<bool>(type: "boolean", nullable: false),
                    AcknowledgedById = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    AcknowledgedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonitoringAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonitoringAlerts_AspNetUsers_AcknowledgedById",
                        column: x => x.AcknowledgedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MonitoringAlerts_AspNetUsers_WriterId",
                        column: x => x.WriterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MonitoringAlerts_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringAlerts_AcknowledgedById",
                table: "MonitoringAlerts",
                column: "AcknowledgedById");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringAlerts_AlertType",
                table: "MonitoringAlerts",
                column: "AlertType");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringAlerts_CreatedAt",
                table: "MonitoringAlerts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringAlerts_IsAcknowledged",
                table: "MonitoringAlerts",
                column: "IsAcknowledged");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringAlerts_OrderId",
                table: "MonitoringAlerts",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringAlerts_WriterId",
                table: "MonitoringAlerts",
                column: "WriterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MonitoringAlerts");
        }
    }
}
