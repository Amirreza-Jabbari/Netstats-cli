namespace NetStats.Models;

public record GeoInfo
{
    public string Ip { get; init; } = "";
    public string Country { get; init; } = "";
    public string Region { get; init; } = "";
    public string City { get; init; } = "";
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public string Timezone { get; init; } = "";
    public string? Provider { get; init; }
}
