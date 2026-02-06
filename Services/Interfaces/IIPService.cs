using NetStats.Models;

namespace NetStats.Services.Interfaces;

public interface IIPService
{
    Task<IpInfo> GetPublicIpInfoAsync(CancellationToken ct = default);
}
