using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using System.Text.Json;
using YtApi.Data;
using YtApi.Models;

namespace YtApi.Services;

public class AiGenerationService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AiGenerationService> _logger;

    public AiGenerationService(IServiceScopeFactory scopeFactory, ILogger<AiGenerationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    private async Task<string> CallOpenAiAsync(string systemPrompt, string userPrompt)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var apiKey = await db.Settings.Where(s => s.Key == "OpenAI:ApiKey").Select(s => s.Value).FirstOrDefaultAsync() 
                     ?? throw new Exception("OpenAI API Key ayarlanmamış.");
        var model = await db.Settings.Where(s => s.Key == "OpenAI:Model").Select(s => s.Value).FirstOrDefaultAsync() ?? "gpt-4o-mini";

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        var payload = new
        {
            model = model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            }
        };

        var res = await http.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", payload);
        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadAsStringAsync();
            throw new Exception($"OpenAI Hatası: {err}");
        }

        var data = await JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync());
        return data.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
    }

    public async Task<Topic> GenerateTopicAsync(Pipeline pipeline)
    {
        var sys = "Sen milyonlarca izlenen, profesyonel bir YouTube Shorts içerik üreticisisin. Hedef kitleyi saniyesinde yakalayacak, son derece viral, merak uyandırıcı ve 'tıklama tuzağı (clickbait)' hissiyatı veren ama içi dolu bir YouTube Shorts konusu üret. Başlık çok çarpıcı olmalı. Çıktını SADECE şu JSON formatında ver: {\"title\": \"Viral Başlık\", \"description\": \"Kısa ve merak uyandıran açıklama\"}";
        var prompt = $"Kanal Dili: {pipeline.Language}\nHedef Kitle: {pipeline.TargetAudience}\nAnahtar Kelime/Konu: {pipeline.TopicPrompt}\n\nLütfen bu anahtar kelimeden yola çıkarak inanılmaz dikkat çekici bir Shorts konusu üret.";

        var jsonRes = await CallOpenAiAsync(sys, prompt);
        
        // Temizle (Bazen markdown içinde dönebiliyor)
        jsonRes = jsonRes.Replace("```json", "").Replace("```", "").Trim();
        var data = JsonSerializer.Deserialize<JsonElement>(jsonRes);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var topic = new Topic
        {
            ChannelId = pipeline.ChannelId,
            PipelineId = pipeline.Id,
            Title = data.GetProperty("title").GetString() ?? "İlginç Bir Konu",
            Description = data.GetProperty("description").GetString(),
            Language = pipeline.Language,
            Status = "COMPLETED"
        };

        db.Topics.Add(topic);
        await db.SaveChangesAsync();
        return topic;
    }

    public async Task<Script> GenerateScriptAsync(Pipeline pipeline, Topic topic)
    {
        var stylePrompt = pipeline.ScriptStyle switch
        {
            "CURIOSITY_HOOK" => "İzleyicide aşırı merak uyandıran bir kanca (hook) ile başla.",
            "STORYTELLING" => "Sürükleyici bir hikaye gibi anlat.",
            "MOTIVATIONAL" => "Güçlü, motive edici ve ilham verici kelimeler kullan.",
            "EDUCATIONAL" => "Hap bilgiler ver, eğitici ve net ol.",
            _ => "Dikkat çekici ol."
        };

        var sys = "Sen en iyi YouTube Shorts senaristi ve metin yazarısın. Sadece spikerin/seslendirmenin okuyacağı metni yazacaksın. Kamera açıları veya parantez içi (Gülümser) gibi notlar ASLA YAZMA. Metin tam 50-60 saniyede okunabilecek uzunlukta olmalı (yaklaşık 120-150 kelime). Sadece metni düz metin olarak ver.";
        var prompt = $"Konu Başlığı: {topic.Title}\nKonu Detayı: {topic.Description}\nDil: {pipeline.Language}\nStil: {stylePrompt}\n\nLütfen Shorts metnini yaz.";

        var scriptText = await CallOpenAiAsync(sys, prompt);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var script = new Script
        {
            TopicId = topic.Id,
            ChannelId = pipeline.ChannelId,
            Title = topic.Title + " Senaryosu",
            Content = scriptText,
            Language = pipeline.Language,
            Style = pipeline.ScriptStyle,
            DurationSeconds = 60
        };

        db.Scripts.Add(script);
        await db.SaveChangesAsync();
        return script;
    }

    public async Task<List<string>> GenerateSearchQueriesAsync(Script script)
    {
        var sys = "Sen bir stok video arama asistanısın. Kullanıcının sana verdiği video senaryosunu okuyarak, hikayenin gidişatına uygun 5 farklı Pexels arama terimi (keyword) üret. Her terim tam olarak 1 veya 2 kelimelik İNGİLİZCE bir kelime olmalıdır. ÖNEMLİ UYARI: 'cupping' kelimesi İngilizce'de 'kahve tadımı' anlamına gelir ve kahve videoları çıkar! Hacamat için ASLA sadece 'cupping' veya 'cup' kullanma, mutlaka 'cupping therapy', 'hijama', 'acupuncture', 'spa massage', 'hospital' gibi net tıbbi kelimeler kullan. Kelimeler kesinlikle Pexels'te bulunabilecek genel geçer stok kelimeler olmalı. Çıktını SADECE bir JSON listesi olarak ver. Örnek: [\"cupping therapy\", \"spa massage\", \"back pain\", \"healthy body\", \"relaxing\"]";
        var prompt = $"Senaryo Metni: {script.Content}";

        var jsonRes = await CallOpenAiAsync(sys, prompt);
        jsonRes = jsonRes.Replace("```json", "").Replace("```", "").Trim();
        
        try
        {
            return JsonSerializer.Deserialize<List<string>>(jsonRes) ?? new List<string> { "nature", "business", "city", "technology", "abstract" };
        }
        catch
        {
            return new List<string> { "abstract", "cinematic", "background", "texture", "slow motion" };
        }
    }

    public async Task<List<string>> GenerateTagsAsync(Script script)
    {
        var sys = "Sen bir YouTube SEO uzmanısın. Verilen senaryoya en uygun, arama hacmi yüksek 15 adet YouTube etiketini (tag) üret. Kelimeler boşluk içerebilir. Çıktını SADECE bir JSON listesi olarak ver. Örnek: [\"hacamat nasıl yapılır\", \"sağlık\", \"sırt ağrısı\"]";
        var prompt = $"Senaryo Başlığı: {script.Title}\nSenaryo Metni: {script.Content}";

        var jsonRes = await CallOpenAiAsync(sys, prompt);
        jsonRes = jsonRes.Replace("```json", "").Replace("```", "").Trim();
        
        try
        {
            return JsonSerializer.Deserialize<List<string>>(jsonRes) ?? new List<string> { "shorts", "viral", "trending" };
        }
        catch
        {
            return new List<string> { "shorts", "viral", "trending" };
        }
    }
}
