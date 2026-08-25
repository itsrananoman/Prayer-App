using Microsoft.EntityFrameworkCore;
using Prayer.Models;
using System.IO;

namespace Prayer.Data;

public class PrayerDbContext : DbContext
{
    public DbSet<PrayerTimeRecord> PrayerTimes => Set<PrayerTimeRecord>();
    public DbSet<ManualOverride> ManualOverrides => Set<ManualOverride>();
    public DbSet<UserSetting> UserSettings => Set<UserSetting>();
    public DbSet<DailyVerse> DailyVerses => Set<DailyVerse>();

    private readonly string _dbPath;

    public PrayerDbContext()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var folder = Path.Combine(appData, "PrayerApp");
        Directory.CreateDirectory(folder);
        _dbPath = Path.Combine(folder, "prayer.db");
    }

    public PrayerDbContext(DbContextOptions<PrayerDbContext> options) : base(options)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var folder = Path.Combine(appData, "PrayerApp");
        Directory.CreateDirectory(folder);
        _dbPath = Path.Combine(folder, "prayer.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite($"Data Source={_dbPath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PrayerTimeRecord>()
            .HasIndex(p => p.Date);

        modelBuilder.Entity<ManualOverride>()
            .HasIndex(m => m.PrayerName);

        modelBuilder.Entity<DailyVerse>()
            .HasIndex(v => v.DayOfYear);
    }
}
