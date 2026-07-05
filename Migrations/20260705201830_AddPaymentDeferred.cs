using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarRescue.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentDeferred : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PaymentDeferred",
                table: "Orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentDeferred",
                table: "Orders");
        }
    }
}
