using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarRescue.Migrations
{
    /// <inheritdoc />
    public partial class PendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsReopened",
                table: "SupportTickets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastReplyDate",
                table: "SupportTickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "SupportTickets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "QueueName",
                table: "SupportTickets",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UnreadCount",
                table: "SupportTickets",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsReopened",
                table: "SupportTickets");

            migrationBuilder.DropColumn(
                name: "LastReplyDate",
                table: "SupportTickets");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "SupportTickets");

            migrationBuilder.DropColumn(
                name: "QueueName",
                table: "SupportTickets");

            migrationBuilder.DropColumn(
                name: "UnreadCount",
                table: "SupportTickets");
        }
    }
}
