using System;
using Jellyfin.Plugin.Themerr.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Jellyfin.Plugin.Themerr.Storage.Migrations
{
    /// <summary>
    /// Snapshot of the Themerr database model.
    /// </summary>
    [DbContext(typeof(ThemerrDbContext))]
    public class ThemerrDbContextModelSnapshot : ModelSnapshot
    {
        /// <inheritdoc />
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "9.0.11");

            modelBuilder.Entity("Jellyfin.Plugin.Themerr.Storage.ThemerrMediaItem", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("INTEGER");

                b.Property<DateTime>("CreatedUtc")
                    .HasColumnType("TEXT");

                b.Property<DateTime?>("DownloadedTimestampUtc")
                    .HasColumnType("TEXT");

                b.Property<bool>("InThemerrDb")
                    .HasColumnType("INTEGER");

                b.Property<DateTime?>("InThemerrDbCheckedUtc")
                    .HasColumnType("TEXT");

                b.Property<string>("IssueUrl")
                    .HasColumnType("TEXT");

                b.Property<string>("ItemId")
                    .HasColumnType("TEXT");

                b.Property<string>("ItemKey")
                    .IsRequired()
                    .HasColumnType("TEXT");

                b.Property<string>("ItemName")
                    .IsRequired()
                    .HasColumnType("TEXT");

                b.Property<string>("ItemPath")
                    .HasColumnType("TEXT");

                b.Property<string>("ItemType")
                    .IsRequired()
                    .HasColumnType("TEXT");

                b.Property<int?>("ProductionYear")
                    .HasColumnType("INTEGER");

                b.Property<string>("ThemeMd5")
                    .HasColumnType("TEXT");

                b.Property<string>("ThemePath")
                    .HasColumnType("TEXT");

                b.Property<string>("ThemeProvider")
                    .HasColumnType("TEXT");

                b.Property<string>("TmdbId")
                    .HasColumnType("TEXT");

                b.Property<DateTime>("UpdatedUtc")
                    .HasColumnType("TEXT");

                b.Property<string>("YoutubeThemeUrl")
                    .HasColumnType("TEXT");

                b.HasKey("Id");

                b.HasIndex("ItemKey")
                    .IsUnique();

                b.ToTable("ThemerrMediaItems");
            });
#pragma warning restore 612, 618
        }
    }
}
