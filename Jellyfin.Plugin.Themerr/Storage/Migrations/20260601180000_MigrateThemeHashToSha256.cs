using Jellyfin.Plugin.Themerr.Storage;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Plugin.Themerr.Storage.Migrations
{
    /// <summary>
    /// Replaces legacy theme MD5 storage with SHA-256 theme hash storage.
    /// </summary>
    [DbContext(typeof(ThemerrDbContext))]
    [Migration("20260601180000_MigrateThemeHashToSha256")]
    public partial class MigrateThemeHashToSha256 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ThemeMd5",
                table: "ThemerrMediaItems",
                newName: "ThemeHash");

            migrationBuilder.AddColumn<string>(
                name: "ThemeHashAlgorithm",
                table: "ThemerrMediaItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE \"ThemerrMediaItems\" SET \"ThemeHash\" = NULL, \"ThemeHashAlgorithm\" = NULL " +
                "WHERE \"ThemeHash\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE ""ThemerrMediaItems_Reverted"" (
    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_ThemerrMediaItems"" PRIMARY KEY AUTOINCREMENT,
    ""ItemKey"" TEXT NOT NULL,
    ""ItemId"" TEXT NULL,
    ""ItemType"" TEXT NOT NULL,
    ""ItemName"" TEXT NOT NULL,
    ""ProductionYear"" INTEGER NULL,
    ""ItemPath"" TEXT NULL,
    ""ThemePath"" TEXT NULL,
    ""TmdbId"" TEXT NULL,
    ""ThemeMd5"" TEXT NULL,
    ""ThemeProvider"" TEXT NULL,
    ""InThemerrDb"" INTEGER NOT NULL,
    ""InThemerrDbCheckedUtc"" TEXT NULL,
    ""IssueUrl"" TEXT NULL,
    ""YoutubeThemeUrl"" TEXT NULL,
    ""DownloadedTimestampUtc"" TEXT NULL,
    ""CreatedUtc"" TEXT NOT NULL,
    ""UpdatedUtc"" TEXT NOT NULL
);

INSERT INTO ""ThemerrMediaItems_Reverted"" (
    ""Id"",
    ""ItemKey"",
    ""ItemId"",
    ""ItemType"",
    ""ItemName"",
    ""ProductionYear"",
    ""ItemPath"",
    ""ThemePath"",
    ""TmdbId"",
    ""ThemeMd5"",
    ""ThemeProvider"",
    ""InThemerrDb"",
    ""InThemerrDbCheckedUtc"",
    ""IssueUrl"",
    ""YoutubeThemeUrl"",
    ""DownloadedTimestampUtc"",
    ""CreatedUtc"",
    ""UpdatedUtc""
)
SELECT
    ""Id"",
    ""ItemKey"",
    ""ItemId"",
    ""ItemType"",
    ""ItemName"",
    ""ProductionYear"",
    ""ItemPath"",
    ""ThemePath"",
    ""TmdbId"",
    ""ThemeHash"",
    ""ThemeProvider"",
    ""InThemerrDb"",
    ""InThemerrDbCheckedUtc"",
    ""IssueUrl"",
    ""YoutubeThemeUrl"",
    ""DownloadedTimestampUtc"",
    ""CreatedUtc"",
    ""UpdatedUtc""
FROM ""ThemerrMediaItems"";

DROP TABLE ""ThemerrMediaItems"";

ALTER TABLE ""ThemerrMediaItems_Reverted"" RENAME TO ""ThemerrMediaItems"";

CREATE UNIQUE INDEX ""IX_ThemerrMediaItems_ItemKey"" ON ""ThemerrMediaItems"" (""ItemKey"");
");
        }
    }
}
