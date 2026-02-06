namespace NetStats.Models;

public record PingResult
{
    public double AverageMs { get; init; }
    public double MinMs { get; init; }
    public double MaxMs { get; init; }
    public double JitterMs { get; init; } // std dev
    public double PacketLossPercent { get; init; }
    public int Samples { get; init; }
}
