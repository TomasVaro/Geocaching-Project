using Microsoft.EntityFrameworkCore.Migrations;

namespace Geocaching.Migrations
{
    public partial class AddedJointabel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FoundGeocache",
                columns: table => new
                {
                    PersonID = table.Column<int>(nullable: false),
                    GeocacheID = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoundGeocache", x => new { x.PersonID, x.GeocacheID });
                    table.ForeignKey(
                        name: "FK_FoundGeocache_Geocache_GeocacheID",
                        column: x => x.GeocacheID,
                        principalTable: "Geocache",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FoundGeocache_Person_PersonID",
                        column: x => x.PersonID,
                        principalTable: "Person",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FoundGeocache_GeocacheID",
                table: "FoundGeocache",
                column: "GeocacheID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FoundGeocache");
        }
    }
}
