namespace NetStats.Models;

public record SpeedResult
{
    public double DownloadMbps { get; init; }
    public double UploadMbps { get; init; }
    public TimeSpan Duration { get; init; }
    public string? ServerUsed { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
