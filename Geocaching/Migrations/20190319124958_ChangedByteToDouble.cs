using Microsoft.EntityFrameworkCore.Migrations;

namespace Geocaching.Migrations
{
    public partial class ChangedByteToDouble : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "Longitude",
                table: "Person",
                nullable: false,
                oldClrType: typeof(float));

            migrationBuilder.AlterColumn<double>(
                name: "Latitude",
                table: "Person",
                nullable: false,
                oldClrType: typeof(float));

            migrationBuilder.AlterColumn<double>(
                name: "Longitude",
                table: "Geocache",
                nullable: false,
                oldClrType: typeof(float));

            migrationBuilder.AlterColumn<double>(
                name: "Latitude",
                table: "Geocache",
                nullable: false,
                oldClrType: typeof(float));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<float>(
                name: "Longitude",
                table: "Person",
                nullable: false,
                oldClrType: typeof(double));

            migrationBuilder.AlterColumn<float>(
                name: "Latitude",
                table: "Person",
                nullable: false,
                oldClrType: typeof(double));

            migrationBuilder.AlterColumn<float>(
                name: "Longitude",
                table: "Geocache",
                nullable: false,
                oldClrType: typeof(double));

            migrationBuilder.AlterColumn<float>(
                name: "Latitude",
                table: "Geocache",
                nullable: false,
                oldClrType: typeof(double));
        }
    }
}
