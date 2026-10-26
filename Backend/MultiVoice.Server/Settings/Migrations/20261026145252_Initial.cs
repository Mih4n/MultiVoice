using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultiVoice.Server.Settings.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SpeakerSettings",
                columns: table => new
                {
                    ListenerXuid = table.Column<string>(type: "TEXT", nullable: false),
                    SpeakerXuid = table.Column<string>(type: "TEXT", nullable: false),
                    Volume = table.Column<float>(type: "REAL", nullable: false),
                    Muted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpeakerSettings", x => new { x.ListenerXuid, x.SpeakerXuid });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpeakerSettings");
        }
    }
}
