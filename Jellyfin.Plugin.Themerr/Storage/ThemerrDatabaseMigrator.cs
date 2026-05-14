using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Jellyfin.Plugin.Themerr.Storage
{
    /// <summary>
    /// Applies Themerr database migrations.
    /// </summary>
    public class ThemerrDatabaseMigrator
    {
        private readonly string _databasePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThemerrDatabaseMigrator"/> class.
        /// </summary>
        /// <param name="databasePath">The sqlite database path.</param>
        public ThemerrDatabaseMigrator(string databasePath)
        {
            _databasePath = databasePath;
        }

        /// <summary>
        /// Applies all pending migrations.
        /// </summary>
        /// <returns>True when the sqlite database file was created during migration; otherwise, false.</returns>
        public bool MigrateUp()
        {
            var databaseExists = File.Exists(_databasePath);
            var databaseDirectory = Path.GetDirectoryName(_databasePath);
            if (!string.IsNullOrEmpty(databaseDirectory))
            {
                Directory.CreateDirectory(databaseDirectory);
            }

            using (var context = new ThemerrDbContext(_databasePath))
            {
                context.Database.Migrate();
            }

            return !databaseExists && File.Exists(_databasePath);
        }

        /// <summary>
        /// Reverts all migrations.
        /// </summary>
        public void MigrateDown()
        {
            using (var context = new ThemerrDbContext(_databasePath))
            {
                context.GetService<IMigrator>().Migrate("0");
            }
        }
    }
}
