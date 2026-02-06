using System.Text;
using NetStats.Models;

namespace NetStats.Output;

public class MarkdownFormatter : IOutputFormatter
{
    private static string FormatMs(double ms)
    {
        if (double.IsNaN(ms)) return "-";
        if (double.IsInfinity(ms)) return "timeout";
        return $"{ms:N0}";
    }

    public void WriteIp(IpInfo ip)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# IP Information");
        sb.AppendLine();
        sb.AppendLine($"- **IP**: {ip.Query}");
        sb.AppendLine($"- **ISP**: {ip.Isp}");
        sb.AppendLine($"- **ASN**: {ip.As}");
        sb.AppendLine($"- **Proxy**: {ip.Proxy}");
        sb.AppendLine($"- **Mobile**: {ip.Mobile}");
        sb.AppendLine($"- **Hosting**: {ip.Hosting}");
        var md = sb.ToString();
        Console.WriteLine(md);
        SaveToFile("ip", md);
    }

    public void WriteGeo(GeoInfo geo, IpInfo? ip = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Geolocation");
        sb.AppendLine();
        sb.AppendLine($"- **IP**: {geo.Ip}");
        sb.AppendLine($"- **Country**: {geo.Country}");
        sb.AppendLine($"- **Region**: {geo.Region}");
        sb.AppendLine($"- **City**: {geo.City}");
        sb.AppendLine($"- **Coordinates**: {geo.Latitude}, {geo.Longitude}");
        sb.AppendLine($"- **Timezone**: {geo.Timezone}");
        if (!string.IsNullOrWhiteSpace(geo.Provider))
            sb.AppendLine($"- **Provider**: {geo.Provider}");
        var md = sb.ToString();
        Console.WriteLine(md);
        SaveToFile("geo", md);
    }

    public void WriteSpeed(SpeedResult speed, PingResult ping)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Speed Test");
        sb.AppendLine();
        sb.AppendLine($"- **Download**: {speed.DownloadMbps} Mbps");
        sb.AppendLine($"- **Upload**: {speed.UploadMbps} Mbps");
        sb.AppendLine($"- **Ping (avg)**: {ping.AverageMs} ms");
        sb.AppendLine($"- **Jitter**: {ping.JitterMs} ms");
        sb.AppendLine($"- **Loss**: {ping.PacketLossPercent}%");
        sb.AppendLine($"- **Server**: {speed.ServerUsed}");
        var md = sb.ToString();
        Console.WriteLine(md);
        SaveToFile("speed", md);
    }

    public void WriteFullReport(IpInfo ip, GeoInfo geo, SpeedResult speed, PingResult ping, DnsResult dns, IEnumerable<TracerouteHop> trace)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# IP Information");
        sb.AppendLine();
        sb.AppendLine($"- **IP**: {ip.Query}");
        sb.AppendLine($"- **ISP**: {ip.Isp}");
        sb.AppendLine($"- **ASN**: {ip.As}");
        sb.AppendLine($"- **Proxy**: {ip.Proxy}");
        sb.AppendLine($"- **Mobile**: {ip.Mobile}");
        sb.AppendLine($"- **Hosting**: {ip.Hosting}");
        sb.AppendLine();

        sb.AppendLine("# Geolocation");
        sb.AppendLine();
        sb.AppendLine($"- **IP**: {geo.Ip}");
        sb.AppendLine($"- **Country**: {geo.Country}");
        sb.AppendLine($"- **Region**: {geo.Region}");
        sb.AppendLine($"- **City**: {geo.City}");
        sb.AppendLine($"- **Coordinates**: {geo.Latitude}, {geo.Longitude}");
        sb.AppendLine($"- **Timezone**: {geo.Timezone}");
        if (!string.IsNullOrWhiteSpace(geo.Provider))
            sb.AppendLine($"- **Provider**: {geo.Provider}");
        sb.AppendLine();

        sb.AppendLine("# Speed Test");
        sb.AppendLine();
        sb.AppendLine($"- **Download**: {speed.DownloadMbps} Mbps");
        sb.AppendLine($"- **Upload**: {speed.UploadMbps} Mbps");
        sb.AppendLine($"- **Ping (avg)**: {ping.AverageMs} ms");
        sb.AppendLine($"- **Jitter**: {ping.JitterMs} ms");
        sb.AppendLine($"- **Loss**: {ping.PacketLossPercent}%");
        sb.AppendLine($"- **Server**: {speed.ServerUsed}");
        sb.AppendLine();

        sb.AppendLine("## DNS");
        sb.AppendLine();
        sb.AppendLine($"- Queried Host: {dns.QueriedHost}");
        sb.AppendLine($"- Query Time: {FormatMs(dns.QueryTimeMs)} ms");
        sb.AppendLine($"- System DNS: {string.Join(", ", dns.SystemDnsServers)}");
        sb.AppendLine();

        sb.AppendLine("### Public DNS comparison");
        sb.AppendLine();
        if (dns.PublicDnsComparison.Length == 0)
        {
            sb.AppendLine("- (no data)");
        }
        else
        {
            foreach (var p in dns.PublicDnsComparison)
            {
                sb.AppendLine($"- {p.Server}: {FormatMs(p.TimeMs)} ms");
            }
        }
        sb.AppendLine();

        sb.AppendLine("## Traceroute (top 12)");
        sb.AppendLine();
        sb.AppendLine("| Hop | Address | Hostname | Avg RTT (ms) |");
        sb.AppendLine("|-----|---------|----------|--------------|");
        var anyHop = false;
        foreach (var hop in (trace ?? Array.Empty<TracerouteHop>()).Take(12))
        {
            anyHop = true;
            sb.AppendLine($"| {hop.Hop} | {hop.Address ?? "-"} | {hop.Hostname ?? "-"} | {(hop.AvgRtt?.TotalMilliseconds.ToString("N0") ?? "-")} |");
        }
        if (!anyHop)
        {
            sb.AppendLine("| - | - | - | - |");
        }

        var md = sb.ToString();
        Console.WriteLine(md);
        SaveToFile("all", md);
    }

    private static void SaveToFile(string prefix, string content)
    {
        var dir = Path.Combine(Environment.CurrentDirectory, "netstats-output");
        Directory.CreateDirectory(dir);
        var fileName = $"{prefix}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.md";
        var path = Path.Combine(dir, fileName);
        File.WriteAllText(path, content);
        Console.WriteLine($"Saved to: {Path.GetFullPath(path)}");
    }
}
