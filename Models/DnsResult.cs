namespace NetStats.Models;

public record DnsResult
{
    public string QueriedHost { get; init; } = "";
    public double QueryTimeMs { get; init; }
    public string[] SystemDnsServers { get; init; } = Array.Empty<string>();
    public (string Server, double TimeMs)[] PublicDnsComparison { get; init; } = Array.Empty<(string, double)>();
}
