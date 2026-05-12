using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Plugin.Themerr.Storage
{
    /// <summary>
    /// Entity Framework Core context for Themerr plugin data.
    /// </summary>
    public class ThemerrDbContext : DbContext
    {
        private readonly string _databasePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThemerrDbContext"/> class.
        /// </summary>
        /// <param name="databasePath">The sqlite database path.</param>
        public ThemerrDbContext(string databasePath)
        {
            _databasePath = databasePath;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ThemerrDbContext"/> class.
        /// </summary>
        /// <param name="options">The context options.</param>
        public ThemerrDbContext(DbContextOptions<ThemerrDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Gets or sets Themerr media item metadata.
        /// </summary>
        public DbSet<ThemerrMediaItem> MediaItems { get; set; }

        /// <inheritdoc />
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite($"Data Source={_databasePath}");
            }
        }

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var mediaItem = modelBuilder.Entity<ThemerrMediaItem>();

            mediaItem.ToTable("ThemerrMediaItems");
            mediaItem.HasKey(item => item.Id);
            mediaItem.HasIndex(item => item.ItemKey).IsUnique();

            mediaItem.Property(item => item.ItemKey).IsRequired();
            mediaItem.Property(item => item.ItemType).IsRequired();
            mediaItem.Property(item => item.CreatedUtc).IsRequired();
            mediaItem.Property(item => item.UpdatedUtc).IsRequired();
        }
    }
}
