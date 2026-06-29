using Microsoft.EntityFrameworkCore;
using Job_Finder_Bot.Models;

namespace Job_Finder_Bot.Data;

public class JobFinderDbContext : DbContext
{
    public DbSet<JobPosting> JobPostings { get; set; }

    // Creates a SQLite database in a persistent location
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Store database in user's AppData folder so it persists across rebuilds
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dbFolder = Path.Combine(appDataPath, "JobFinderBot");

        // Create folder if it doesn't exist
        Directory.CreateDirectory(dbFolder);

        var dbPath = Path.Combine(dbFolder, "jobfinder.db");
        optionsBuilder.UseSqlite($"Data Source={dbPath}");

        Console.WriteLine($"[Database] Using database at: {dbPath}");
    }

    // Driver function to configure the model (JobPosting) and its constraints
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<JobPosting>(entity =>
        {
            entity.HasKey(e => e.Id); // Primary key
            entity.Property(e => e.JobTitle).IsRequired(); // NOT NULL
            entity.Property(e => e.Company).IsRequired(); // NOT NULL
            entity.Property(e => e.SourceUrl).IsRequired(); // NOT NULL
            entity.Property(e => e.UniqueJobId).IsRequired(); // NOT NULL
            entity.HasIndex(e => e.UniqueJobId).IsUnique(); // UNIQUE Constraint (Avoid duplicate postings based on normalized job ID)
        });
    }
}
