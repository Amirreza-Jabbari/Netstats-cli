namespace NetStats.Models;

public record TracerouteHop
{
    public int Hop { get; init; }
    public string? Address { get; init; }
    public string? Hostname { get; init; }
    public TimeSpan? AvgRtt { get; init; }
}
