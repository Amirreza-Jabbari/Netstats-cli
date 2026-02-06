namespace NetStats.Models;

public record IpInfo
{
    public string? Status { get; init; }
    public string? Message { get; init; }
    public string? Query { get; init; } // IP
    public string? Isp { get; init; }
    public string? As { get; init; } // ASN string
    public string? Country { get; init; }
    public string? RegionName { get; init; }
    public string? City { get; init; }
    public double? Lat { get; init; }
    public double? Lon { get; init; }
    public string? Timezone { get; init; }
    public bool Mobile { get; init; }
    public bool Proxy { get; init; }
    public bool Hosting { get; init; }
    public DateTime RetrievedAt { get; init; } = DateTime.UtcNow;
}
