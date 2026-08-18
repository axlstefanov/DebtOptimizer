using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DebtOptimizer.Migrations
{
    /// <inheritdoc />
    public partial class AddDebtPayoffDeadline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "PayoffDeadline",
                table: "Debts",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PayoffDeadline",
                table: "Debts");
        }
    }
}
