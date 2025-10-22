using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BreadCharts.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVoting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubmittedSongs",
                columns: table => new
                {
                    TrackId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TrackName = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    SubmittedByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmittedSongs", x => x.TrackId);
                });

            migrationBuilder.CreateTable(
                name: "SongVotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TrackId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    VotedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SongVotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SongVotes_SubmittedSongs_TrackId",
                        column: x => x.TrackId,
                        principalTable: "SubmittedSongs",
                        principalColumn: "TrackId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SongVotes_TrackId_UserId",
                table: "SongVotes",
                columns: new[] { "TrackId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SongVotes");

            migrationBuilder.DropTable(
                name: "SubmittedSongs");
        }
    }
}
