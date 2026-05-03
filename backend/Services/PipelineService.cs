using Microsoft.EntityFrameworkCore;
using YtApi.Data;
using YtApi.Models;
using YtApi.Controllers;

namespace YtApi.Services;

public class PipelineService
{
    private readonly AppDbContext _db;
    public PipelineService(AppDbContext db) { _db = db; }

    public async Task<List<object>> List() =>
        await _db.Pipelines.Include(p => p.Channel).OrderByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                p.Id, p.Name, p.IsActive, p.ScriptStyle, p.Language, p.FrequencyInMinutes,
                p.VoiceId, p.AiProvider, p.VideoType, p.TopicPrompt, p.TargetAudience,
                p.ScheduleStartHour, p.ScheduleEndHour, p.CreatedAt,
                channel = p.Channel == null ? null : new { p.Channel.Id, p.Channel.Title, p.Channel.ThumbnailUrl }
            })
            .ToListAsync<object>();

    public async Task<Pipeline> Create(Pipeline p)
    {
        _db.Pipelines.Add(p);
        await _db.SaveChangesAsync();
        return p;
    }

    public async Task<bool> Delete(Guid id)
    {
        var p = await _db.Pipelines.FindAsync(id);
        if (p == null) return false;
        _db.Pipelines.Remove(p);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> Update(Guid id, PipelineController.CreateDto dto)
    {
        var p = await _db.Pipelines.FindAsync(id);
        if (p == null) return false;

        p.Name = dto.Name;
        if (dto.ScriptStyle != null) p.ScriptStyle = dto.ScriptStyle;
        if (dto.Language != null) p.Language = dto.Language;
        if (dto.FrequencyInMinutes != null) p.FrequencyInMinutes = dto.FrequencyInMinutes.Value;
        if (dto.VoiceId != null) p.VoiceId = dto.VoiceId;
        if (dto.AiProvider != null) p.AiProvider = dto.AiProvider;
        if (dto.VideoType != null) p.VideoType = dto.VideoType;
        p.TopicPrompt = dto.TopicPrompt;
        p.TargetAudience = dto.TargetAudience;
        if (dto.ScheduleStartHour != null) p.ScheduleStartHour = dto.ScheduleStartHour.Value;
        if (dto.ScheduleEndHour != null) p.ScheduleEndHour = dto.ScheduleEndHour.Value;
        p.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> Start(Guid id)
    {
        var pipeline = await _db.Pipelines.FindAsync(id);
        if (pipeline == null) return false;
        pipeline.IsActive = true;
        // Queue a topic generation job
        _db.PipelineJobs.Add(new PipelineJob
        {
            PipelineId = pipeline.Id,
            Step = "TOPIC_GENERATION",
            Status = "QUEUED",
        });
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task StartAll()
    {
        var ids = await _db.Pipelines.Where(p => p.IsActive).Select(p => p.Id).ToListAsync();
        foreach (var id in ids) await Start(id);
    }

    public async Task<bool> Stop(Guid id)
    {
        var p = await _db.Pipelines.FindAsync(id);
        if (p == null) return false;
        p.IsActive = false;

        // Cancel all queued jobs for this pipeline
        var queuedJobs = await _db.PipelineJobs
            .Where(j => j.PipelineId == id && j.Status == "QUEUED")
            .ToListAsync();
            
        foreach (var job in queuedJobs)
        {
            job.Status = "CANCELLED";
            job.CompletedAt = DateTime.UtcNow;
            job.Payload = "{\"reason\": \"Pipeline stopped by user\"}";
        }

        await _db.SaveChangesAsync();
        return true;
    }
}
