using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using DnsClient;
using NetStats.Models;
using NetStats.Services.Interfaces;

namespace NetStats.Services;

public class DnsService : IDnsService
{
    public async Task<DnsResult> RunDnsDiagnosticsAsync(string host, CancellationToken ct = default)
    {
        var systemServers = GetSystemDnsServers();
        var lookupOptions = new LookupClientOptions(systemServers.Select(IPAddress.Parse).ToArray())
        {
            Timeout = TimeSpan.FromSeconds(5),
            Retries = 1
        };
        var lookup = new LookupClient(lookupOptions);

        double timeMs = double.NaN;
        try
        {
            var sw = Stopwatch.StartNew();
            var _ = await lookup.QueryAsync(host, QueryType.A, cancellationToken: ct);
            sw.Stop();
            timeMs = sw.Elapsed.TotalMilliseconds;
        }
        catch
        {
            timeMs = double.PositiveInfinity;
        }

        var publics = new[] { "8.8.8.8", "1.1.1.1", "9.9.9.9" };
        var comparisons = new List<(string, double)>();
        foreach (var p in publics)
        {
            try
            {
                var l = new LookupClient(new LookupClientOptions(IPAddress.Parse(p)) { Timeout = TimeSpan.FromSeconds(3), Retries = 0 });
                var sw2 = Stopwatch.StartNew();
                var rr = await l.QueryAsync(host, QueryType.A, cancellationToken: ct);
                sw2.Stop();
                comparisons.Add((p, sw2.Elapsed.TotalMilliseconds));
            }
            catch
            {
                comparisons.Add((p, double.PositiveInfinity));
            }
        }

        return new DnsResult
        {
            QueriedHost = host,
            QueryTimeMs = timeMs,
            SystemDnsServers = systemServers,
            PublicDnsComparison = comparisons.ToArray()
        };
    }

    private static string[] GetSystemDnsServers()
    {
        var servers = new List<string>();
        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            var props = ni.GetIPProperties();
            foreach (var dns in props.DnsAddresses)
            {
                servers.Add(dns.ToString());
            }
        }

        if (!servers.Any())
        {
            // fallback
            servers.Add("8.8.8.8");
            servers.Add("1.1.1.1");
        }

        return servers.Distinct().ToArray();
    }
}
