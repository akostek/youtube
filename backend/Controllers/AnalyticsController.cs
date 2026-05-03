using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YtApi.Data;
using YtApi.Services;

namespace YtApi.Controllers;

[ApiController, Route("api/analytics"), Authorize]
public class AnalyticsController(AppDbContext db, YouTubeIntegrationService ytService) : ControllerBase
{
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var channels = await db.Channels.ToListAsync();
        ulong totalViews = 0, totalLikes = 0, totalComments = 0;

        foreach (var channel in channels)
        {
            var uploads = await db.Uploads.Where(u => u.ChannelId == channel.Id && u.Status == "PUBLISHED" && u.YoutubeVideoId != null).ToListAsync();
            foreach (var upload in uploads)
            {
                try
                {
                    var (views, likes, comments) = await ytService.GetVideoStatsAsync(channel.Id, upload.YoutubeVideoId!);
                    totalViews += views;
                    totalLikes += likes;
                    totalComments += comments;
                }
                catch
                {
                    // Ignore errors for individual videos
                }
            }
        }

        var publishedCount = await db.Uploads.CountAsync(u => u.Status == "PUBLISHED");
        return Ok(new { totalViews, totalLikes, totalComments, publishedCount });
    }

    [HttpGet("videos")]
    public async Task<IActionResult> GetVideos()
    {
        var uploads = await db.Uploads
            .Include(u => u.Channel)
            .Where(u => u.Status == "PUBLISHED" && u.YoutubeVideoId != null)
            .OrderByDescending(u => u.CreatedAt)
            .Take(50)
            .Select(u => new
            {
                u.Id, u.YoutubeVideoId, u.Title, u.CreatedAt, ChannelTitle = u.Channel!.Title, u.ChannelId
            })
            .ToListAsync();

        var result = new List<object>();
        foreach (var u in uploads)
        {
            ulong views = 0, likes = 0, comments = 0;
            try
            {
                var stats = await ytService.GetVideoStatsAsync(u.ChannelId, u.YoutubeVideoId!);
                views = stats.ViewCount;
                likes = stats.LikeCount;
                comments = stats.CommentCount;
            }
            catch {}

            result.Add(new
            {
                u.Id, u.YoutubeVideoId, u.Title, u.CreatedAt, u.ChannelTitle, views, likes, comments
            });
        }

        return Ok(result);
    }
}
