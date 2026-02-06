using NetStats.Models;

namespace NetStats.Services.Interfaces;

public interface IGeoService
{
    Task<GeoInfo> GetGeoForIpAsync(string? ip, CancellationToken ct = default);
}
