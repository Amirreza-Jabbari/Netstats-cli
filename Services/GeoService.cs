using System.Net.Http.Json;
using System.Text.Json;
using NetStats.Models;
using NetStats.Services.Interfaces;

namespace NetStats.Services;

public class GeoService : IGeoService
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };

    public async Task<GeoInfo> GetGeoForIpAsync(string? ip, CancellationToken ct = default)
    {
        var target = string.IsNullOrWhiteSpace(ip) ? "json" : $"{ip}";
        var url = $"http://ip-api.com/json/{target}?fields=query,country,regionName,city,lat,lon,timezone,isp";
        using var res = await _http.GetAsync(url, ct);
        res.EnsureSuccessStatusCode();

        var json = await res.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        if (json.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("Geo lookup failed");
        }

        double GetDouble(string name)
        {
            if (json.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.Number)
            {
                return prop.GetDouble();
            }
            return 0;
        }

        string GetString(string name)
        {
            return json.TryGetProperty(name, out var prop)
                ? prop.GetString() ?? string.Empty
                : string.Empty;
        }

        return new GeoInfo
        {
            Ip = GetString("query"),
            Country = GetString("country"),
            Region = GetString("regionName"),
            City = GetString("city"),
            Latitude = GetDouble("lat"),
            Longitude = GetDouble("lon"),
            Timezone = GetString("timezone"),
            Provider = GetString("isp")
        };
    }
}
