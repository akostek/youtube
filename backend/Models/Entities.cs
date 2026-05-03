using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YtApi.Models;

// ======== USER ========
[Table("users")]
public class User
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Name { get; set; } = "Admin";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// ======== CHANNEL ========
[Table("channels")]
public class Channel
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public string YoutubeId { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public DateTime? TokenExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string DefaultVoice { get; set; } = "tr-TR-AhmetNeural";
    public string DefaultLanguage { get; set; } = "tr";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<Pipeline> Pipelines { get; set; } = [];
    public List<Video> Videos { get; set; } = [];
    public List<Upload> Uploads { get; set; } = [];
}

// ======== PIPELINE ========
[Table("pipelines")]
public class Pipeline
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public Guid ChannelId { get; set; }
    [ForeignKey("ChannelId")] public Channel? Channel { get; set; }
    public string ScriptStyle { get; set; } = "CURIOSITY_HOOK"; // CURIOSITY_HOOK, STORYTELLING, MOTIVATIONAL, EDUCATIONAL
    public string Language { get; set; } = "tr";
    public int FrequencyInMinutes { get; set; } = 60;
    public bool IsActive { get; set; } = true;
    public string VoiceId { get; set; } = "tr-TR-AhmetNeural";
    public string AiProvider { get; set; } = "openai"; // openai, gemini
    public string VideoType { get; set; } = "stock"; // stock, text_overlay
    public string? TopicPrompt { get; set; }
    public string? TargetAudience { get; set; }
    public int ScheduleStartHour { get; set; } = 9;
    public int ScheduleEndHour { get; set; } = 21;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<Topic> Topics { get; set; } = [];
    public List<PipelineJob> Jobs { get; set; } = [];
}

// ======== TOPIC ========
[Table("topics")]
public class Topic
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public Guid ChannelId { get; set; }
    public Guid? PipelineId { get; set; }
    [ForeignKey("PipelineId")] public Pipeline? Pipeline { get; set; }
    public string Status { get; set; } = "PENDING"; // PENDING, IN_PROGRESS, COMPLETED, FAILED
    public string Language { get; set; } = "tr";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Script> Scripts { get; set; } = [];
}

// ======== SCRIPT ========
[Table("scripts")]
public class Script
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TopicId { get; set; }
    [ForeignKey("TopicId")] public Topic? Topic { get; set; }
    public Guid ChannelId { get; set; }
    public string Content { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Style { get; set; } = "CURIOSITY_HOOK";
    public string Language { get; set; } = "tr";
    public int DurationSeconds { get; set; } = 60;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<VoiceOver> VoiceOvers { get; set; } = [];
}

// ======== VOICEOVER ========
[Table("voiceovers")]
public class VoiceOver
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ScriptId { get; set; }
    [ForeignKey("ScriptId")] public Script? Script { get; set; }
    public string FilePath { get; set; } = "";
    public string VoiceId { get; set; } = "";
    public int? DurationMs { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Video> Videos { get; set; } = [];
}

// ======== VIDEO ========
[Table("videos")]
public class Video
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ScriptId { get; set; }
    [ForeignKey("ScriptId")] public Script? Script { get; set; }
    public Guid VoiceOverId { get; set; }
    [ForeignKey("VoiceOverId")] public VoiceOver? VoiceOver { get; set; }
    public Guid ChannelId { get; set; }
    [ForeignKey("ChannelId")] public Channel? Channel { get; set; }
    public string? FilePath { get; set; }
    public string? ThumbnailPath { get; set; }
    public int? DurationSeconds { get; set; }
    public string Status { get; set; } = "PENDING"; // PENDING, RENDERING, COMPLETED, FAILED
    public double RenderProgress { get; set; } = 0;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<Upload> Uploads { get; set; } = [];
}

// ======== UPLOAD ========
[Table("uploads")]
public class Upload
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VideoId { get; set; }
    [ForeignKey("VideoId")] public Video? Video { get; set; }
    public Guid ChannelId { get; set; }
    [ForeignKey("ChannelId")] public Channel? Channel { get; set; }
    public string? YoutubeVideoId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Status { get; set; } = "PENDING"; // PENDING, UPLOADING, PUBLISHED, FAILED
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

// ======== PIPELINE JOB ========
[Table("pipeline_jobs")]
public class PipelineJob
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PipelineId { get; set; }
    [ForeignKey("PipelineId")] public Pipeline? Pipeline { get; set; }
    public string Step { get; set; } = ""; // TOPIC_GENERATION, SCRIPT_GENERATION, VOICE_GENERATION, VIDEO_RENDERING, YOUTUBE_UPLOAD
    public string Status { get; set; } = "QUEUED"; // QUEUED, PROCESSING, COMPLETED, FAILED
    public string? Payload { get; set; } // JSON
    public string? Result { get; set; }  // JSON
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; } = 0;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

// ======== SETTING ========
[Table("settings")]
public class Setting
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
    public string Category { get; set; } = "general";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
