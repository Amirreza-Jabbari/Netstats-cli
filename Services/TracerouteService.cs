using System.Net;
using System.Net.NetworkInformation;
using NetStats.Models;
using NetStats.Services.Interfaces;

namespace NetStats.Services;

public class TracerouteService : ITracerouteService
{
    public async Task<IEnumerable<TracerouteHop>> TracerouteAsync(string hostname, CancellationToken ct = default)
    {
        var hops = new List<TracerouteHop>();
        const int maxHops = 30;
        const int probesPerHop = 2;
        var destIps = (await Dns.GetHostAddressesAsync(hostname))
            .Where(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            .ToArray();

        var dest = destIps.FirstOrDefault()?.ToString() ?? hostname;

        for (int ttl = 1; ttl <= maxHops && !ct.IsCancellationRequested; ttl++)
        {
            var rtts = new List<long>();
            string? lastAddr = null;
            for (int p = 0; p < probesPerHop; p++)
            {
                try
                {
                    using var ping = new Ping();
                    var po = new PingOptions(ttl, true);
                    var reply = await ping.SendPingAsync(hostname, 2000, new byte[32], po);
                    if (reply.Status == IPStatus.Success || reply.Status == IPStatus.TtlExpired || reply.Status == IPStatus.TimedOut)
                    {
                        if (reply.Address != null)
                        {
                            lastAddr = reply.Address.ToString();
                        }

                        if (reply.Status == IPStatus.Success)
                        {
                            rtts.Add(reply.RoundtripTime);
                        }
                        else if (reply.Status == IPStatus.TtlExpired)
                        {
                            rtts.Add(reply.RoundtripTime);
                        }
                    }
                }
                catch
                {
                    // ignore per-probe error
                }
            }

            if (lastAddr == null)
            {
                hops.Add(new TracerouteHop { Hop = ttl, Address = "*", Hostname = null, AvgRtt = null });
            }
            else
            {
                double avgMs = rtts.Any() ? rtts.Average() : 0;
                string hostName = "";
                try
                {
                    var entry = await Dns.GetHostEntryAsync(lastAddr);
                    hostName = entry.HostName;
                }
                catch
                {
                    hostName = lastAddr;
                }

                hops.Add(new TracerouteHop
                {
                    Hop = ttl,
                    Address = lastAddr,
                    Hostname = hostName,
                    AvgRtt = rtts.Any() ? TimeSpan.FromMilliseconds(avgMs) : null
                });

                if (destIps.Contains(IPAddress.Parse(lastAddr)))
                {
                    break;
                }
            }
        }

        return hops;
    }
}
