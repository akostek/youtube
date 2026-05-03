using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YtApi.Data;

namespace YtApi.Controllers;

[ApiController, Route("api/channels"), Authorize]
public class ChannelController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List() =>
        Ok(await db.Channels.Select(c => new
        {
            c.Id, c.YoutubeId, c.Title, c.ThumbnailUrl, c.IsActive, c.DefaultVoice, c.DefaultLanguage, c.CreatedAt,
            videoCount = c.Videos.Count, uploadCount = c.Uploads.Count
        }).ToListAsync());

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var ch = await db.Channels.FindAsync(id);
        if (ch == null) return NotFound();
        db.Channels.Remove(ch);
        await db.SaveChangesAsync();
        return Ok(new { message = "Kanal silindi" });
    }

    [HttpGet("oauth/connect")]
    [AllowAnonymous]
    public async Task<IActionResult> Connect([FromServices] IConfiguration config, [FromServices] AppDbContext db)
    {
        var clientId = await db.Settings.Where(s => s.Key == "Google:ClientId").Select(s => s.Value).FirstOrDefaultAsync() ?? config["Google:ClientId"];
        var redirectUri = await db.Settings.Where(s => s.Key == "Google:RedirectUri").Select(s => s.Value).FirstOrDefaultAsync() ?? config["Google:RedirectUri"];
        
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(redirectUri))
            return BadRequest("Google API bilgileri eksik (Ayarlar panelini kontrol edin).");

        var scope = "https://www.googleapis.com/auth/youtube https://www.googleapis.com/auth/youtube.upload https://www.googleapis.com/auth/youtube.readonly https://www.googleapis.com/auth/youtube.force-ssl";
        var url = $"https://accounts.google.com/o/oauth2/v2/auth?client_id={clientId}&redirect_uri={redirectUri}&response_type=code&scope={Uri.EscapeDataString(scope)}&access_type=offline&prompt=consent";
        return Redirect(url);
    }

    [HttpGet("oauth/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback([FromQuery] string code, [FromServices] IConfiguration config, [FromServices] AppDbContext db)
    {
        if (string.IsNullOrEmpty(code)) return BadRequest("Kod bulunamadı");

        var clientId = await db.Settings.Where(s => s.Key == "Google:ClientId").Select(s => s.Value).FirstOrDefaultAsync() ?? config["Google:ClientId"];
        var clientSecret = await db.Settings.Where(s => s.Key == "Google:ClientSecret").Select(s => s.Value).FirstOrDefaultAsync() ?? config["Google:ClientSecret"];
        var redirectUri = await db.Settings.Where(s => s.Key == "Google:RedirectUri").Select(s => s.Value).FirstOrDefaultAsync() ?? config["Google:RedirectUri"];

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(redirectUri))
            return BadRequest("Google API bilgileri eksik (Ayarlar panelini kontrol edin).");

        using var http = new HttpClient();
        
        // 1. Exchange code for tokens
        var tokenRes = await http.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            {"code", code}, {"client_id", clientId}, {"client_secret", clientSecret},
            {"redirect_uri", redirectUri}, {"grant_type", "authorization_code"}
        }));
        
        if (!tokenRes.IsSuccessStatusCode)
            return BadRequest("Token alınamadı: " + await tokenRes.Content.ReadAsStringAsync());

        var tokenData = await System.Text.Json.JsonDocument.ParseAsync(await tokenRes.Content.ReadAsStreamAsync());
        var accessToken = tokenData.RootElement.GetProperty("access_token").GetString();
        var refreshToken = tokenData.RootElement.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
        var expiresIn = tokenData.RootElement.GetProperty("expires_in").GetInt32();

        // 2. Get YouTube Channel info
        http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var ytRes = await http.GetAsync("https://www.googleapis.com/youtube/v3/channels?part=snippet&mine=true");
        
        if (!ytRes.IsSuccessStatusCode)
            return BadRequest("Kanal bilgisi alınamadı");

        var ytData = await System.Text.Json.JsonDocument.ParseAsync(await ytRes.Content.ReadAsStreamAsync());
        
        if (!ytData.RootElement.TryGetProperty("items", out var items) || items.GetArrayLength() == 0)
        {
            return BadRequest("Seçtiğiniz Google hesabına bağlı bir YouTube kanalı bulunamadı. Lütfen önce YouTube'a girip bir kanal oluşturun veya kanalı olan bir hesap seçin.");
        }

        var channelNode = items[0];
        var ytId = channelNode.GetProperty("id").GetString();
        var snippet = channelNode.GetProperty("snippet");
        var title = snippet.GetProperty("title").GetString();
        var desc = snippet.GetProperty("description").GetString();
        var thumb = snippet.GetProperty("thumbnails").GetProperty("default").GetProperty("url").GetString();

        // 3. Save or update in DB
        var channel = await db.Channels.FirstOrDefaultAsync(c => c.YoutubeId == ytId);
        if (channel == null)
        {
            channel = new YtApi.Models.Channel { YoutubeId = ytId!, Title = title! };
            db.Channels.Add(channel);
        }
        
        channel.Title = title!;
        channel.Description = desc;
        channel.ThumbnailUrl = thumb;
        channel.AccessToken = accessToken!;
        if (refreshToken != null) channel.RefreshToken = refreshToken;
        channel.TokenExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn);
        channel.IsActive = true;

        await db.SaveChangesAsync();

        var frontendUrl = config["FrontendUrl"] ?? "http://localhost:5173";
        return Redirect($"{frontendUrl}/channels");
    }
}
