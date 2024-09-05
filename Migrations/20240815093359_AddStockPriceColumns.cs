using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockPriceApi.Migrations
{
    /// <inheritdoc />
    public partial class AddStockPriceColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Change",
                table: "StockPrices",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ChangePercent",
                table: "StockPrices",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "High",
                table: "StockPrices",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Low",
                table: "StockPrices",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Open",
                table: "StockPrices",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Volume",
                table: "StockPrices",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Change",
                table: "StockPrices");

            migrationBuilder.DropColumn(
                name: "ChangePercent",
                table: "StockPrices");

            migrationBuilder.DropColumn(
                name: "High",
                table: "StockPrices");

            migrationBuilder.DropColumn(
                name: "Low",
                table: "StockPrices");

            migrationBuilder.DropColumn(
                name: "Open",
                table: "StockPrices");

            migrationBuilder.DropColumn(
                name: "Volume",
                table: "StockPrices");
        }
    }
}
