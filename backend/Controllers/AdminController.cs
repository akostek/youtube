using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YtApi.Data;
using YtApi.Models;

namespace YtApi.Controllers;

[ApiController, Route("api/admin"), Authorize]
public class AdminController(AppDbContext db) : ControllerBase
{
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings() =>
        Ok(await db.Settings.OrderBy(s => s.Category).ThenBy(s => s.Key).ToListAsync());

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSetting([FromBody] SettingDto dto)
    {
        var setting = await db.Settings.FirstOrDefaultAsync(s => s.Key == dto.Key);
        if (setting == null)
        {
            setting = new Setting { Key = dto.Key, Value = dto.Value, Category = dto.Category ?? "general" };
            db.Settings.Add(setting);
        }
        else
        {
            setting.Value = dto.Value;
            setting.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync();
        return Ok(setting);
    }

    [HttpGet("health")]
    [AllowAnonymous]
    public async Task<IActionResult> Health()
    {
        var channels = await db.Channels.CountAsync();
        var pipelines = await db.Pipelines.CountAsync(p => p.IsActive);
        var pending = await db.PipelineJobs.CountAsync(j => j.Status == "QUEUED" || j.Status == "PROCESSING");
        return Ok(new { status = "ok", channels, activePipelines = pipelines, pendingJobs = pending, time = DateTime.UtcNow });
    }

    public record SettingDto(string Key, string Value, string? Category);
}
