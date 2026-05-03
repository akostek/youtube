using Microsoft.EntityFrameworkCore;
using YtApi.Models;

namespace YtApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Channel> Channels => Set<Channel>();
    public DbSet<Pipeline> Pipelines => Set<Pipeline>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<Script> Scripts => Set<Script>();
    public DbSet<VoiceOver> VoiceOvers => Set<VoiceOver>();
    public DbSet<Video> Videos => Set<Video>();
    public DbSet<Upload> Uploads => Set<Upload>();
    public DbSet<PipelineJob> PipelineJobs => Set<PipelineJob>();
    public DbSet<Setting> Settings => Set<Setting>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        m.Entity<User>().HasIndex(u => u.Email).IsUnique();
        m.Entity<Channel>().HasIndex(c => c.YoutubeId).IsUnique();
        m.Entity<Setting>().HasIndex(s => s.Key).IsUnique();
        m.Entity<PipelineJob>().HasIndex(j => new { j.PipelineId, j.Step, j.Status });
    }
}
