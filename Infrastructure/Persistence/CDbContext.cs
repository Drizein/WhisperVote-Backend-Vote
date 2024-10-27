using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence;

public class CDbContext : DbContext
{
    private readonly ILogger<CDbContext> _logger = default!;

    public DbSet<Survey> Surveys { get; set; }
    public DbSet<KeyPair> KeyPairs { get; set; }
    public DbSet<Vote> Votes { get; set; }
    public DbSet<Option> Options { get; set; }
    public DbSet<RequestChangeRole> RequestChangeRoles { get; set; }
    public DbSet<ReportSurvey> ReportSurvey { get; set; }

    public CDbContext()
    {
    }

    public CDbContext(DbContextOptions<CDbContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        _logger?.LogInformation("Inside of CDBContext with path {}", Directory.GetCurrentDirectory());
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettingsMigrations.json", false, true)
            .Build();
        options.UseMySQL(configuration.GetConnectionString("Default")!);
    }

    public DbSet<Survey> UserVotes => Set<Survey>();

    public async Task<bool> SaveAllChangesAsync()
    {
        _logger?.LogDebug("\n{output}", ChangeTracker.DebugView.LongView);

        var result = await SaveChangesAsync();

        _logger?.LogDebug("SaveChanges {result}", result);
        _logger?.LogDebug("\n{output}", ChangeTracker.DebugView.LongView);
        return result > 0;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Survey>(entity =>
        {
            entity.Navigation(survey => survey.Options).AutoInclude();
            entity.HasMany(s => s.Options);

            entity.OwnsMany(e => e.Tags);
        });

        modelBuilder.Entity<Option>(entity =>
        {
            entity.Navigation(option => option.Votes).AutoInclude();
            entity.HasMany(o => o.Votes)
                .WithOne(v => v.Option)
                .HasForeignKey(v => v.OptionId);
        });

        modelBuilder.Entity<ReportSurvey>(entity =>
            {
                entity.Navigation(reportSurvey => reportSurvey.Survey).AutoInclude();
                entity.HasOne(rs => rs.Survey)
                    .WithMany()
                    .HasForeignKey(rs => rs.SurveyId);
            }
        );

        base.OnModelCreating(modelBuilder);
    }
}