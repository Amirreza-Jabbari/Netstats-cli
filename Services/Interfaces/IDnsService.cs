using NetStats.Models;

namespace NetStats.Services.Interfaces;

public interface IDnsService
{
    Task<DnsResult> RunDnsDiagnosticsAsync(string host, CancellationToken ct = default);
}
