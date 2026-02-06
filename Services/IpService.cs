using System.Net.Http.Json;
using NetStats.Models;
using NetStats.Services.Interfaces;

namespace NetStats.Services;

public class IpService : IIPService
{
    private readonly HttpClient _http;

    public IpService()
    {
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    }

    public async Task<IpInfo> GetPublicIpInfoAsync(CancellationToken ct = default)
    {
        var url = "http://ip-api.com/json/?fields=status,message,query,isp,as,country,regionName,city,lat,lon,timezone,mobile,proxy,hosting";
        using var res = await _http.GetAsync(url, ct);
        res.EnsureSuccessStatusCode();
        var json = await res.Content.ReadFromJsonAsync<IpInfo>(cancellationToken: ct);
        if (json is null)
            throw new InvalidOperationException("Failed to parse IP service response.");
        return json with { RetrievedAt = DateTime.UtcNow };
    }
}
