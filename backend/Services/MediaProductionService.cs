using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using YtApi.Data;
using YtApi.Models;

namespace YtApi.Services;

public class MediaProductionService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MediaProductionService> _logger;
    private readonly string _storagePath;

    public MediaProductionService(IServiceScopeFactory scopeFactory, IConfiguration config, ILogger<MediaProductionService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _storagePath = config["Storage:Path"] ?? Path.Combine(Directory.GetCurrentDirectory(), "storage");
        Directory.CreateDirectory(_storagePath);
    }

    private async Task ExecuteCommandAsync(string command, string arguments, Action<string>? onLog = null)
    {
        var tcs = new TaskCompletionSource<int>();
        var errorLog = new System.Text.StringBuilder();

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
            EnableRaisingEvents = true
        };

        process.OutputDataReceived += (s, e) => 
        { 
            if (!string.IsNullOrEmpty(e.Data) && onLog != null) onLog(e.Data); 
        };
        
        process.ErrorDataReceived += (s, e) => 
        { 
            if (!string.IsNullOrEmpty(e.Data)) 
            {
                errorLog.AppendLine(e.Data);
                if (onLog != null) onLog(e.Data); 
            }
        };

        process.Exited += (sender, args) =>
        {
            if (process.ExitCode == 0) tcs.SetResult(0);
            else tcs.SetException(new Exception($"{command} failed with exit code {process.ExitCode}. Error: {errorLog}"));
            process.Dispose();
        };

        process.Start();
        
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await tcs.Task;
    }

    public async Task<VoiceOver> GenerateVoiceAsync(Script script, string voiceId)
    {
        var tempTxtPath = Path.Combine(_storagePath, $"{script.Id}.txt");
        var mp3Path = Path.Combine(_storagePath, $"{script.Id}.mp3");
        var vttPath = Path.Combine(_storagePath, $"{script.Id}.vtt");

        await File.WriteAllTextAsync(tempTxtPath, script.Content);

        _logger.LogInformation("Generating TTS and Subtitles for script {Id} with voice {VoiceId}", script.Id, voiceId);
        
        var edgeTtsPath = @"C:\PythonEnv\venv\Scripts\edge-tts.exe";
        await ExecuteCommandAsync(edgeTtsPath, $"-f \"{tempTxtPath}\" --voice \"{voiceId}\" --write-media \"{mp3Path}\" --write-subtitles \"{vttPath}\"");

        File.Delete(tempTxtPath);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var vo = new VoiceOver
        {
            ScriptId = script.Id,
            FilePath = mp3Path,
            VoiceId = voiceId
        };
        db.VoiceOvers.Add(vo);
        await db.SaveChangesAsync();

        return vo;
    }

    public async Task<string> DownloadBackgroundVideoAsync(List<string> keywords)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var apiKey = await db.Settings.Where(s => s.Key == "Pexels:ApiKey").Select(s => s.Value).FirstOrDefaultAsync();

        var bgPath = Path.Combine(_storagePath, $"bg_merged_{Guid.NewGuid()}.mp4");

        if (string.IsNullOrEmpty(apiKey))
        {
            using var httpFallback = new HttpClient();
            var bytes = await httpFallback.GetByteArrayAsync("https://www.w3schools.com/html/mov_bbb.mp4");
            await File.WriteAllBytesAsync(bgPath, bytes);
            return bgPath;
        }

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add("Authorization", apiKey);

        var tempFiles = new List<string>();
        var ffmpegPath = Path.Combine(Directory.GetCurrentDirectory(), "ffmpeg.exe");
        var rnd = new Random();

        // Try to download 1 random top video for each keyword
        foreach (var keyword in keywords.Take(5))
        {
            var searchUrl = $"https://api.pexels.com/videos/search?query={Uri.EscapeDataString(keyword)}&orientation=portrait&size=large&per_page=15";
            var res = await http.GetAsync(searchUrl);
            
            if (!res.IsSuccessStatusCode) continue;

            var data = await System.Text.Json.JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync());
            var videos = data.RootElement.GetProperty("videos");
            int videoCount = videos.GetArrayLength();
            if (videoCount == 0) continue;

            // Take a random video from the results to ensure variety across different videos
            var selectedVideo = videos[rnd.Next(videoCount)];
            var videoFiles = selectedVideo.GetProperty("video_files");
            
            string videoUrl = "";
            foreach (var file in videoFiles.EnumerateArray())
            {
                if (file.GetProperty("quality").GetString() == "hd")
                {
                    videoUrl = file.GetProperty("link").GetString()!;
                    break;
                }
            }
            if (string.IsNullOrEmpty(videoUrl)) videoUrl = videoFiles[0].GetProperty("link").GetString()!;

            var videoBytes = await http.GetByteArrayAsync(videoUrl);
            var rawTempPath = Path.Combine(_storagePath, $"raw_{Guid.NewGuid()}.mp4");
            var normTempPath = Path.Combine(_storagePath, $"norm_{Guid.NewGuid()}.mp4");
            
            await File.WriteAllBytesAsync(rawTempPath, videoBytes);

            var normalizeArgs = $"-i \"{rawTempPath}\" -vf \"scale=1080:1920:force_original_aspect_ratio=increase,crop=1080:1920,fps=25\" -c:v libx264 -preset veryfast -crf 23 -threads 2 -an -y \"{normTempPath}\"";
            await ExecuteCommandAsync(ffmpegPath, normalizeArgs);

            tempFiles.Add(normTempPath);
            if (File.Exists(rawTempPath)) File.Delete(rawTempPath);
        }

        if (tempFiles.Count == 0)
        {
            // Fallback
            using var httpFallback = new HttpClient();
            var bytes = await httpFallback.GetByteArrayAsync("https://www.w3schools.com/html/mov_bbb.mp4");
            await File.WriteAllBytesAsync(bgPath, bytes);
            return bgPath;
        }

        // Concatenate normalized videos
        var concatTxtPath = Path.Combine(_storagePath, $"concat_{Guid.NewGuid()}.txt");
        var concatContent = string.Join("\n", tempFiles.Select(f => $"file '{Path.GetFullPath(f).Replace("\\", "/")}'"));
        await File.WriteAllTextAsync(concatTxtPath, concatContent);

        var concatArgs = $"-f concat -safe 0 -i \"{concatTxtPath}\" -c copy -y \"{bgPath}\"";
        await ExecuteCommandAsync(ffmpegPath, concatArgs);

        // Cleanup intermediate files
        if (File.Exists(concatTxtPath)) File.Delete(concatTxtPath);
        foreach (var tf in tempFiles)
        {
            if (File.Exists(tf)) File.Delete(tf);
        }

        return bgPath;
    }

    public async Task<Video> RenderVideoAsync(Script script, VoiceOver vo, List<string> searchQueries, Action<string>? onLog = null)
    {
        _logger.LogInformation("Rendering video for script {Id} with queries", script.Id);

        // 1. Download Background Video using dynamic semantic queries
        var bgPath = await DownloadBackgroundVideoAsync(searchQueries);

        var outPath = Path.Combine(_storagePath, $"final_{script.Id}.mp4");
        var vttPath = Path.Combine(_storagePath, $"{script.Id}.vtt");

        // Use local ffmpeg
        var ffmpegPath = Path.Combine(Directory.GetCurrentDirectory(), "ffmpeg.exe");

        // 2. FFmpeg mix (Without hardcoded subtitles)
        // Added -threads 2 to prevent out of memory (OOM) errors on low RAM machines.
        var ffmpegArgs = $"-stream_loop -1 -i \"{bgPath}\" -i \"{vo.FilePath}\" -c:v libx264 -c:a aac -shortest -vf \"format=yuv420p\" -threads 2 -max_muxing_queue_size 9999 \"{outPath}\" -y";
        
        await ExecuteCommandAsync(ffmpegPath, ffmpegArgs, onLog);

        File.Delete(bgPath);
        if (File.Exists(vttPath)) File.Delete(vttPath);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var video = new Video
        {
            ScriptId = script.Id,
            VoiceOverId = vo.Id,
            ChannelId = script.ChannelId,
            FilePath = outPath,
            Status = "COMPLETED"
        };
        db.Videos.Add(video);
        await db.SaveChangesAsync();

        return video;
    }
}
