using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultiVoice.Server.Settings.Migrations
{
    /// <inheritdoc />
    public partial class VoiceBans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VoiceBans",
                columns: table => new
                {
                    Xuid = table.Column<string>(type: "TEXT", nullable: false),
                    Until = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoiceBans", x => x.Xuid);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VoiceBans");
        }
    }
}
