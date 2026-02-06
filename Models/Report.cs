using System.Collections.Generic;

namespace NetStats.Models;

public record Report
{
    public IpInfo? Ip { get; init; }
    public GeoInfo? Geo { get; init; }
    public SpeedResult? Speed { get; init; }
    public PingResult? Ping { get; init; }
    public DnsResult? Dns { get; init; }
    public IEnumerable<TracerouteHop>? Traceroute { get; init; }
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
}
