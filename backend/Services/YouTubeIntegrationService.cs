using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Microsoft.EntityFrameworkCore;
using YtApi.Data;

namespace YtApi.Services;

public class YouTubeIntegrationService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<YouTubeIntegrationService> _logger;

    public YouTubeIntegrationService(IServiceScopeFactory scopeFactory, IConfiguration config, ILogger<YouTubeIntegrationService> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    private async Task<YouTubeService> GetYouTubeServiceAsync(Guid channelId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var channel = await db.Channels.FindAsync(channelId);
        if (channel == null) throw new Exception("Kanal bulunamadı");

        var clientId = await db.Settings.Where(s => s.Key == "Google:ClientId").Select(s => s.Value).FirstOrDefaultAsync();
        var clientSecret = await db.Settings.Where(s => s.Key == "Google:ClientSecret").Select(s => s.Value).FirstOrDefaultAsync();

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            throw new Exception("Google API bilgileri eksik (Ayarlar panelini kontrol edin).");

        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = clientId,
                ClientSecret = clientSecret
            }
        });

        var token = new TokenResponse
        {
            AccessToken = channel.AccessToken,
            RefreshToken = channel.RefreshToken,
            ExpiresInSeconds = (long?)Math.Max(0, (channel.TokenExpiresAt?.Subtract(DateTime.UtcNow).TotalSeconds) ?? 0),
            IssuedUtc = DateTime.UtcNow
        };

        var credential = new UserCredential(flow, channel.YoutubeId, token);

        return new YouTubeService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "YtAutomation"
        });
    }

    public async Task<string> UploadVideoAsync(Guid channelId, string filePath, string title, string description, List<string> tags, Action<double> onProgress)
    {
        var service = await GetYouTubeServiceAsync(channelId);
        
        // Append top 3 tags as hashtags to the description
        var hashTags = string.Join(" ", tags.Take(3).Select(t => "#" + t.Replace(" ", "")));
        var fullDescription = $"{description}\n\n{hashTags}";

        var video = new Google.Apis.YouTube.v3.Data.Video
        {
            Snippet = new Google.Apis.YouTube.v3.Data.VideoSnippet
            {
                Title = title,
                Description = fullDescription,
                CategoryId = "22", // Blogs
                Tags = tags
            },
            Status = new Google.Apis.YouTube.v3.Data.VideoStatus
            {
                PrivacyStatus = "public"
            }
        };

        using var fileStream = new FileStream(filePath, FileMode.Open);
        var insertRequest = service.Videos.Insert(video, "snippet,status", fileStream, "video/*");
        
        insertRequest.ProgressChanged += (progress) =>
        {
            if (progress.Status == Google.Apis.Upload.UploadStatus.Uploading && progress.BytesSent > 0 && fileStream.Length > 0)
            {
                var pct = (double)progress.BytesSent / fileStream.Length * 100;
                onProgress(Math.Min(100, pct));
            }
        };

        var response = await insertRequest.UploadAsync();
        
        if (response.Status == Google.Apis.Upload.UploadStatus.Failed)
        {
            throw new Exception("Yükleme başarısız: " + response.Exception.Message);
        }

        return insertRequest.ResponseBody?.Id ?? throw new Exception("Video ID alınamadı");
    }

    public async Task<(ulong ViewCount, ulong LikeCount, ulong CommentCount)> GetVideoStatsAsync(Guid channelId, string videoId)
    {
        var service = await GetYouTubeServiceAsync(channelId);
        var req = service.Videos.List("statistics");
        req.Id = videoId;
        var res = await req.ExecuteAsync();

        if (res.Items.Count > 0)
        {
            var stat = res.Items[0].Statistics;
            return (stat.ViewCount ?? 0, stat.LikeCount ?? 0, stat.CommentCount ?? 0);
        }
        return (0, 0, 0);
    }

    public async Task ProcessAndReplyToCommentsAsync(Guid channelId, string videoId)
    {
        var service = await GetYouTubeServiceAsync(channelId);
        
        // Fetch top level comments
        var req = service.CommentThreads.List("snippet");
        req.VideoId = videoId;
        req.Order = CommentThreadsResource.ListRequest.OrderEnum.Time; // newest first
        req.MaxResults = 10;
        
        var res = await req.ExecuteAsync();

        foreach (var thread in res.Items)
        {
            var topComment = thread.Snippet.TopLevelComment;
            var text = topComment.Snippet.TextOriginal;
            var replyCount = thread.Snippet.TotalReplyCount ?? 0;

            // If no replies yet, let's reply
            if (replyCount == 0)
            {
                var replyText = await GenerateAiReplyAsync(text);
                if (!string.IsNullOrEmpty(replyText))
                {
                    var reply = new Google.Apis.YouTube.v3.Data.Comment
                    {
                        Snippet = new Google.Apis.YouTube.v3.Data.CommentSnippet
                        {
                            ParentId = topComment.Id,
                            TextOriginal = replyText
                        }
                    };
                    
                    var insertReq = service.Comments.Insert(reply, "snippet");
                    await insertReq.ExecuteAsync();
                }
            }
        }
    }

    private async Task<string> GenerateAiReplyAsync(string userComment)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var apiKey = await db.Settings.Where(s => s.Key == "OpenAI:ApiKey").Select(s => s.Value).FirstOrDefaultAsync();
        var model = await db.Settings.Where(s => s.Key == "OpenAI:Model").Select(s => s.Value).FirstOrDefaultAsync() ?? "gpt-4o-mini";
        
        if (string.IsNullOrEmpty(apiKey)) return "";

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        var payload = new
        {
            model = model,
            messages = new[]
            {
                new { role = "system", content = "Sen bir YouTube kanalı sahibisin. Kullanıcı yorumlarına kısa, samimi, esprili ve teşekkür eden cevaplar ver. 1-2 cümleyi geçme." },
                new { role = "user", content = userComment }
            }
        };

        var res = await http.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", payload);
        if (res.IsSuccessStatusCode)
        {
            var data = await System.Text.Json.JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync());
            return data.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
        }
        return "";
    }
}
