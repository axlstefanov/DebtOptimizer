using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DebtOptimizer.Migrations
{
    /// <inheritdoc />
    public partial class AddProfilePayoffStrategy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PayoffStrategy",
                table: "FinancialProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TargetDebtName",
                table: "FinancialProfiles",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PayoffStrategy",
                table: "FinancialProfiles");

            migrationBuilder.DropColumn(
                name: "TargetDebtName",
                table: "FinancialProfiles");
        }
    }
}
