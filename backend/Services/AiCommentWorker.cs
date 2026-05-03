using Microsoft.EntityFrameworkCore;
using YtApi.Data;

namespace YtApi.Services;

public class AiCommentWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AiCommentWorker> _logger;

    public AiCommentWorker(IServiceScopeFactory scopeFactory, ILogger<AiCommentWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AiCommentWorker started");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var ytService = scope.ServiceProvider.GetRequiredService<YouTubeIntegrationService>();

                // Get recently published videos (limit 10 for safety)
                var uploads = await db.Uploads
                    .Where(u => u.Status == "PUBLISHED" && u.YoutubeVideoId != null)
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(10)
                    .ToListAsync(stoppingToken);

                foreach (var upload in uploads)
                {
                    try
                    {
                        await ytService.ProcessAndReplyToCommentsAsync(upload.ChannelId, upload.YoutubeVideoId!);
                    }
                    catch (Google.GoogleApiException gex) when (gex.HttpStatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        _logger.LogWarning("Kanal yetkisi eksik veya video yoruma kapalı. Lütfen yetkileri veya videoyu kontrol edin. (Video: {VideoId})", upload.YoutubeVideoId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing comments for video {VideoId}", upload.YoutubeVideoId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AiCommentWorker error");
            }

            // Run every 1 minute
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
