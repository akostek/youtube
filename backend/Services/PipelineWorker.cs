using Microsoft.EntityFrameworkCore;
using YtApi.Data;
using YtApi.Models;

namespace YtApi.Services;

public class PipelineWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PipelineWorker> _logger;

    public PipelineWorker(IServiceScopeFactory scopeFactory, ILogger<PipelineWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PipelineWorker started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var jobs = await db.PipelineJobs
                    .Where(j => j.Status == "QUEUED")
                    .OrderBy(j => j.CreatedAt)
                    .Take(5)
                    .ToListAsync(stoppingToken);

                if (jobs.Count > 0)
                {
                    _logger.LogInformation("Processing {Count} jobs in parallel", jobs.Count);

                    foreach (var job in jobs)
                    {
                        job.Status = "PROCESSING";
                        job.StartedAt = DateTime.UtcNow;
                    }
                    await db.SaveChangesAsync(stoppingToken);

                    var tasks = jobs.Select(job => ProcessJob(job.Id, stoppingToken));
                    await Task.WhenAll(tasks);
                }

                // 2. Schedule new jobs based on FrequencyInMinutes
                var activePipelines = await db.Pipelines.Where(p => p.IsActive).ToListAsync(stoppingToken);
                foreach (var p in activePipelines)
                {
                    // Ensure it is within schedule hours
                    var currentHour = DateTime.UtcNow.AddHours(3).Hour; // assuming TR time or configurable
                    if (currentHour < p.ScheduleStartHour || currentHour >= p.ScheduleEndHour) continue;

                    var lastJob = await db.PipelineJobs
                        .Where(j => j.PipelineId == p.Id && j.Step == "TOPIC_GENERATION")
                        .OrderByDescending(j => j.CreatedAt)
                        .FirstOrDefaultAsync(stoppingToken);
                        
                    bool shouldTrigger = false;
                    if (lastJob == null) 
                    {
                        shouldTrigger = true;
                    }
                    else if ((DateTime.UtcNow - lastJob.CreatedAt).TotalMinutes >= p.FrequencyInMinutes)
                    {
                        shouldTrigger = true;
                    }

                    if (shouldTrigger)
                    {
                        db.PipelineJobs.Add(new PipelineJob
                        {
                            PipelineId = p.Id,
                            Step = "TOPIC_GENERATION",
                            Status = "QUEUED"
                        });
                        await db.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation("Scheduled new TOPIC_GENERATION for pipeline {Id} (Freq: {Freq}m)", p.Id, p.FrequencyInMinutes);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PipelineWorker error");
            }

            await Task.Delay(5000, stoppingToken);
        }
    }

    private async Task ProcessJob(Guid jobId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ytService = scope.ServiceProvider.GetRequiredService<YouTubeIntegrationService>();
        var aiService = scope.ServiceProvider.GetRequiredService<AiGenerationService>();
        var mediaService = scope.ServiceProvider.GetRequiredService<MediaProductionService>();
        
        var job = await db.PipelineJobs.Include(j => j.Pipeline).FirstOrDefaultAsync(j => j.Id == jobId, ct);
        if (job == null) return;

        // Ensure the pipeline is still active before processing
        if (job.Pipeline == null || !job.Pipeline.IsActive)
        {
            _logger.LogInformation("Pipeline {Id} is stopped. Cancelling job {JobId}", job.PipelineId, jobId);
            job.Status = "CANCELLED";
            job.CompletedAt = DateTime.UtcNow;
            job.Payload = "{\"reason\": \"Pipeline was stopped before execution\"}";
            await db.SaveChangesAsync(ct);
            return;
        }

        try
        {
            _logger.LogInformation("Processing job {Id} step {Step}", job.Id, job.Step);

            if (job.Step == "TOPIC_GENERATION")
            {
                var topic = await aiService.GenerateTopicAsync(job.Pipeline!);
                job.Result = $"{{\"topicId\": \"{topic.Id}\"}}";
            }
            else if (job.Step == "SCRIPT_GENERATION")
            {
                var topic = await db.Topics.OrderByDescending(t => t.CreatedAt).FirstOrDefaultAsync(t => t.PipelineId == job.Pipeline.Id, ct);
                if (topic == null) throw new Exception("Konu bulunamadı");
                var script = await aiService.GenerateScriptAsync(job.Pipeline!, topic);
                job.Result = $"{{\"scriptId\": \"{script.Id}\"}}";
            }
            else if (job.Step == "VOICE_GENERATION")
            {
                var script = await db.Scripts.OrderByDescending(s => s.CreatedAt).FirstOrDefaultAsync(s => s.Topic!.PipelineId == job.Pipeline.Id, ct);
                if (script == null) throw new Exception("Senaryo bulunamadı");
                var vo = await mediaService.GenerateVoiceAsync(script, job.Pipeline!.VoiceId);
                job.Result = $"{{\"voiceOverId\": \"{vo.Id}\"}}";
            }
            else if (job.Step == "VIDEO_RENDERING")
            {
                var script = await db.Scripts.OrderByDescending(s => s.CreatedAt).FirstOrDefaultAsync(s => s.Topic!.PipelineId == job.Pipeline.Id, ct);
                var vo = await db.VoiceOvers.OrderByDescending(v => v.CreatedAt).FirstOrDefaultAsync(v => v.ScriptId == script.Id, ct);
                if (script == null || vo == null) throw new Exception("Senaryo veya ses bulunamadı");
                
                var topic = await db.Topics.FindAsync(script.TopicId);
                var searchQueries = await aiService.GenerateSearchQueriesAsync(script);

                DateTime lastUpdate = DateTime.UtcNow;

                var video = await mediaService.RenderVideoAsync(script, vo, searchQueries, (logLine) => 
                {
                    // Throttle updates to DB to avoid thrashing (1 update per second)
                    if ((DateTime.UtcNow - lastUpdate).TotalSeconds > 1.0)
                    {
                        lastUpdate = DateTime.UtcNow;
                        var cleanLog = logLine.Replace("\"", "'").Replace("\\", "/");
                        
                        // Use a separate scope to prevent EF Core concurrency deadlocks
                        using var progressScope = _scopeFactory.CreateScope();
                        var progressDb = progressScope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var progressJob = progressDb.PipelineJobs.Find(jobId);
                        if (progressJob != null)
                        {
                            progressJob.Payload = $"{{\"progress\": 75, \"detail\": \"{cleanLog}\"}}";
                            progressDb.SaveChanges();
                        }
                    }
                });
                job.Result = $"{{\"videoId\": \"{video.Id}\"}}";
            }
            else if (job.Step == "YOUTUBE_UPLOAD")
            {
                var video = await db.Videos.OrderByDescending(v => v.CreatedAt).FirstOrDefaultAsync(v => v.ChannelId == job.Pipeline.ChannelId, ct);
                if (video == null || !File.Exists(video.FilePath)) throw new Exception("Yüklenecek video dosyası bulunamadı");

                var script = await db.Scripts.FindAsync(video.ScriptId);
                var title = script?.Title ?? ("Otomatik Yüklenen Video " + DateTime.UtcNow.ToString("HH:mm"));
                var desc = script?.Content ?? "Bu video yt-automation tarafından yüklendi.";
                var tags = script != null ? await aiService.GenerateTagsAsync(script) : new List<string> { "shorts", "viral" };
                
                var ytVideoId = await ytService.UploadVideoAsync(job.Pipeline!.ChannelId, video.FilePath!, title, desc, tags, (pct) => 
                {
                    // Update progress using a new scope to avoid EF Core locks
                    using var progScope = _scopeFactory.CreateScope();
                    var progDb = progScope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var pJob = progDb.PipelineJobs.Find(jobId);
                    if (pJob != null)
                    {
                        pJob.Payload = $"{{\"progress\": {pct}}}";
                        progDb.SaveChanges();
                    }
                });

                db.Uploads.Add(new Upload
                {
                    VideoId = video.Id,
                    ChannelId = job.Pipeline.ChannelId,
                    YoutubeVideoId = ytVideoId,
                    Title = title,
                    Description = desc,
                    Status = "PUBLISHED"
                });
                
                await db.SaveChangesAsync(ct);

                // CLEANUP: Local files
                try
                {
                    if (File.Exists(video.FilePath)) File.Delete(video.FilePath);
                    var vo = await db.VoiceOvers.FindAsync(video.VoiceOverId);
                    if (vo != null && File.Exists(vo.FilePath)) File.Delete(vo.FilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Dosyalar silinemedi: {Msg}", ex.Message);
                }
            }

            job.Status = "COMPLETED";
            job.Payload = "{\"progress\": 100}";
            job.CompletedAt = DateTime.UtcNow;

            // Chain: queue next step
            var nextStep = job.Step switch
            {
                "TOPIC_GENERATION" => "SCRIPT_GENERATION",
                "SCRIPT_GENERATION" => "VOICE_GENERATION",
                "VOICE_GENERATION" => "VIDEO_RENDERING",
                "VIDEO_RENDERING" => "YOUTUBE_UPLOAD",
                _ => null
            };

            if (nextStep != null)
            {
                db.PipelineJobs.Add(new PipelineJob
                {
                    PipelineId = job.PipelineId,
                    Step = nextStep,
                    Status = "QUEUED",
                });
            }

            await db.SaveChangesAsync(ct);
            _logger.LogInformation("Job {Id} completed, next: {Next}", job.Id, nextStep ?? "DONE");
        }
        catch (Exception ex)
        {
            job.Status = "FAILED";
            job.ErrorMessage = ex.Message;
            await db.SaveChangesAsync(ct);
            _logger.LogError(ex, "Job {Id} failed", job.Id);
        }
    }
}
