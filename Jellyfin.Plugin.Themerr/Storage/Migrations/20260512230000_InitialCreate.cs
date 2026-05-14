using System;
using Jellyfin.Plugin.Themerr.Storage;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Plugin.Themerr.Storage.Migrations
{
    /// <summary>
    /// Creates the initial Themerr sqlite schema.
    /// </summary>
    [DbContext(typeof(ThemerrDbContext))]
    [Migration("20260512230000_InitialCreate")]
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ThemerrMediaItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemKey = table.Column<string>(type: "TEXT", nullable: false),
                    ItemId = table.Column<string>(type: "TEXT", nullable: true),
                    ItemType = table.Column<string>(type: "TEXT", nullable: false),
                    ItemName = table.Column<string>(type: "TEXT", nullable: false),
                    ProductionYear = table.Column<int>(type: "INTEGER", nullable: true),
                    ItemPath = table.Column<string>(type: "TEXT", nullable: true),
                    ThemePath = table.Column<string>(type: "TEXT", nullable: true),
                    TmdbId = table.Column<string>(type: "TEXT", nullable: true),
                    ThemeMd5 = table.Column<string>(type: "TEXT", nullable: true),
                    ThemeProvider = table.Column<string>(type: "TEXT", nullable: true),
                    InThemerrDb = table.Column<bool>(type: "INTEGER", nullable: false),
                    InThemerrDbCheckedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IssueUrl = table.Column<string>(type: "TEXT", nullable: true),
                    YoutubeThemeUrl = table.Column<string>(type: "TEXT", nullable: true),
                    DownloadedTimestampUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThemerrMediaItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ThemerrMediaItems_ItemKey",
                table: "ThemerrMediaItems",
                column: "ItemKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ThemerrMediaItems");
        }
    }
}
