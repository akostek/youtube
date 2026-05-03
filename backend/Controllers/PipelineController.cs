using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YtApi.Models;
using YtApi.Services;

namespace YtApi.Controllers;

[ApiController, Route("api/pipelines"), Authorize]
public class PipelineController(PipelineService svc) : ControllerBase
{
    public record CreateDto(string Name, Guid ChannelId, string? ScriptStyle, string? Language, int? FrequencyInMinutes,
        string? VoiceId, string? AiProvider, string? VideoType, string? TopicPrompt, string? TargetAudience,
        int? ScheduleStartHour, int? ScheduleEndHour);

    [HttpGet]
    public async Task<IActionResult> List() => Ok(await svc.List());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDto dto)
    {
        var p = new Pipeline
        {
            Name = dto.Name, ChannelId = dto.ChannelId,
            ScriptStyle = dto.ScriptStyle ?? "CURIOSITY_HOOK",
            Language = dto.Language ?? "tr",
            FrequencyInMinutes = dto.FrequencyInMinutes ?? 60,
            VoiceId = dto.VoiceId ?? "tr-TR-AhmetNeural",
            AiProvider = dto.AiProvider ?? "openai",
            VideoType = dto.VideoType ?? "stock",
            TopicPrompt = dto.TopicPrompt,
            TargetAudience = dto.TargetAudience,
            ScheduleStartHour = dto.ScheduleStartHour ?? 9,
            ScheduleEndHour = dto.ScheduleEndHour ?? 21,
        };
        return Ok(await svc.Create(p));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateDto dto)
    {
        var success = await svc.Update(id, dto);
        if (!success) return NotFound();
        return Ok(new { message = "Otomasyon güncellendi" });
    }

    [HttpPost("{id}/start")]
    public async Task<IActionResult> Start(Guid id) => await svc.Start(id) ? Ok(new { message = "Başlatıldı" }) : NotFound();

    [HttpPost("start-all")]
    public async Task<IActionResult> StartAll() { await svc.StartAll(); return Ok(new { message = "Tüm akışlar başlatıldı" }); }

    [HttpPost("{id}/stop")]
    public async Task<IActionResult> Stop(Guid id) => await svc.Stop(id) ? Ok(new { message = "Durduruldu" }) : NotFound();

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id) => await svc.Delete(id) ? Ok(new { message = "Silindi" }) : NotFound();
}
