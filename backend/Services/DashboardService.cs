using Microsoft.EntityFrameworkCore;
using YtApi.Data;

namespace YtApi.Services;

public class DashboardService
{
    private readonly AppDbContext _db;
    public DashboardService(AppDbContext db) { _db = db; }

    public async Task<object> GetStats()
    {
        var channels = await _db.Channels.CountAsync();
        var pipelines = await _db.Pipelines.CountAsync(p => p.IsActive);
        var videos = await _db.Videos.CountAsync(v => v.Status == "COMPLETED");
        var published = await _db.Uploads.CountAsync(u => u.Status == "PUBLISHED");
        var pending = await _db.PipelineJobs.CountAsync(j => j.Status == "QUEUED" || j.Status == "PROCESSING");
        var failed = await _db.PipelineJobs.CountAsync(j => j.Status == "FAILED");
        return new { channels, activePipelines = pipelines, videosGenerated = videos, videosPublished = published, pendingJobs = pending, failedJobs = failed };
    }

    public async Task<object> GetActiveJobs() =>
        await _db.PipelineJobs
            .Where(j => j.Status == "QUEUED" || j.Status == "PROCESSING")
            .Include(j => j.Pipeline).ThenInclude(p => p!.Channel)
            .OrderByDescending(j => j.CreatedAt)
            .Take(50)
            .Select(j => new
            {
                j.Id, j.Step, j.Status, j.CreatedAt, j.StartedAt,
                pipeline = j.Pipeline == null ? null : new { j.Pipeline.Name, channel = j.Pipeline.Channel == null ? null : new { j.Pipeline.Channel.Title } }
            })
            .ToListAsync();

    public async Task<object> GetLogs() =>
        await _db.PipelineJobs
            .Include(j => j.Pipeline).ThenInclude(p => p!.Channel)
            .OrderByDescending(j => j.UpdatedAt)
            .Take(50)
            .Select(j => new
            {
                j.Id, j.Step, j.Status, j.ErrorMessage, j.UpdatedAt, j.StartedAt, j.CompletedAt,
                pipeline = j.Pipeline == null ? null : new { j.Pipeline.Name, channel = j.Pipeline.Channel == null ? null : new { j.Pipeline.Channel.Title } }
            })
            .ToListAsync();
}
