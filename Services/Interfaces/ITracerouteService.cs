using NetStats.Models;

namespace NetStats.Services.Interfaces;

public interface ITracerouteService
{
    Task<IEnumerable<TracerouteHop>> TracerouteAsync(string hostname, CancellationToken ct = default);
}
