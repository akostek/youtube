using YtApi.Data;
using YtApi.Models;

namespace YtApi.Services;

public static class SeedData
{
    public static async Task Initialize(AppDbContext db, IConfiguration config)
    {
        var email = config["Admin:Email"] ?? "admin@yt.local";
        var password = config["Admin:Password"] ?? "admin123";

        if (!db.Users.Any(u => u.Email == email))
        {
            db.Users.Add(new User
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Name = "Admin"
            });
            await db.SaveChangesAsync();
        }

        var defaultSettings = new Dictionary<string, string>
        {
            { "OpenAI:ApiKey", config["OpenAI:ApiKey"] ?? "" },
            { "OpenAI:Model", config["OpenAI:Model"] ?? "gpt-4o-mini" },
            { "Gemini:ApiKey", config["Gemini:ApiKey"] ?? "" },
            { "Gemini:Model", config["Gemini:Model"] ?? "gemini-2.0-flash" },
            { "Google:ClientId", config["Google:ClientId"] ?? "" },
            { "Google:ClientSecret", config["Google:ClientSecret"] ?? "" },
            { "Google:RedirectUri", config["Google:RedirectUri"] ?? "" },
            { "Pexels:ApiKey", config["Pexels:ApiKey"] ?? "" },
            { "System:DefaultLanguage", "tr" },
            { "System:DefaultVoice", "tr-TR-AhmetNeural" },
            { "System:PostsPerDay", "3" }
        };

        foreach (var (key, val) in defaultSettings)
        {
            if (!db.Settings.Any(s => s.Key == key))
            {
                db.Settings.Add(new Setting
                {
                    Key = key,
                    Value = val,
                    Category = key.Split(':')[0].ToLower()
                });
            }
        }
        await db.SaveChangesAsync();
    }
}
