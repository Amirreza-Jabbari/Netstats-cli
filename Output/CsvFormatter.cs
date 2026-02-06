using System.Text;
using NetStats.Models;

namespace NetStats.Output;

public class CsvFormatter : IOutputFormatter
{
    public void WriteIp(IpInfo ip)
    {
        var sb = new StringBuilder();
        sb.AppendLine("ip,isp,asn,country,region,city,lat,lon,timezone");
        sb.AppendLine($"{ip.Query},{Escape(ip.Isp)},{Escape(ip.As)},{Escape(ip.Country)},{Escape(ip.RegionName)},{Escape(ip.City)},{ip.Lat},{ip.Lon},{Escape(ip.Timezone)}");
        var csv = sb.ToString();
        Console.WriteLine(csv);
        SaveToFile("ip", csv);
    }

    public void WriteGeo(GeoInfo geo, IpInfo? ip = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("ip,country,region,city,lat,lon,timezone,provider");
        sb.AppendLine($"{geo.Ip},{Escape(geo.Country)},{Escape(geo.Region)},{Escape(geo.City)},{geo.Latitude},{geo.Longitude},{Escape(geo.Timezone)},{Escape(geo.Provider)}");
        var csv = sb.ToString();
        Console.WriteLine(csv);
        SaveToFile("geo", csv);
    }

    public void WriteSpeed(SpeedResult speed, PingResult ping)
    {
        var sb = new StringBuilder();
        sb.AppendLine("download_mbps,upload_mbps,duration_s,server");
        sb.AppendLine($"{speed.DownloadMbps},{speed.UploadMbps},{speed.Duration.TotalSeconds},{Escape(speed.ServerUsed)}");
        sb.AppendLine();
        sb.AppendLine("ping_avg_ms,ping_jitter_ms,packet_loss_pct");
        sb.AppendLine($"{ping.AverageMs},{ping.JitterMs},{ping.PacketLossPercent}");
        var csv = sb.ToString();
        Console.WriteLine(csv);
        SaveToFile("speed", csv);
    }

    public void WriteFullReport(IpInfo ip, GeoInfo geo, SpeedResult speed, PingResult ping, DnsResult dns, IEnumerable<TracerouteHop> trace)
    {
        var sb = new StringBuilder();

        // IP
        sb.AppendLine("ip,isp,asn,country,region,city,lat,lon,timezone");
        sb.AppendLine($"{ip.Query},{Escape(ip.Isp)},{Escape(ip.As)},{Escape(ip.Country)},{Escape(ip.RegionName)},{Escape(ip.City)},{ip.Lat},{ip.Lon},{Escape(ip.Timezone)}");
        sb.AppendLine();

        // Geo
        sb.AppendLine("ip,country,region,city,lat,lon,timezone,provider");
        sb.AppendLine($"{geo.Ip},{Escape(geo.Country)},{Escape(geo.Region)},{Escape(geo.City)},{geo.Latitude},{geo.Longitude},{Escape(geo.Timezone)},{Escape(geo.Provider)}");
        sb.AppendLine();

        // Speed + ping
        sb.AppendLine("download_mbps,upload_mbps,duration_s,server");
        sb.AppendLine($"{speed.DownloadMbps},{speed.UploadMbps},{speed.Duration.TotalSeconds},{Escape(speed.ServerUsed)}");
        sb.AppendLine("ping_avg_ms,ping_jitter_ms,packet_loss_pct");
        sb.AppendLine($"{ping.AverageMs},{ping.JitterMs},{ping.PacketLossPercent}");
        sb.AppendLine();

        // DNS
        sb.AppendLine("dns_host,dns_time_ms,system_dns,public_dns,time_ms");
        foreach (var p in dns.PublicDnsComparison)
        {
            sb.AppendLine($"{dns.QueriedHost},{dns.QueryTimeMs},\"{string.Join("|", dns.SystemDnsServers)}\",{p.Server},{p.TimeMs}");
        }
        sb.AppendLine();

        // Traceroute
        sb.AppendLine("hop,address,hostname,avg_rtt_ms");
        foreach (var h in trace)
        {
            sb.AppendLine($"{h.Hop},{h.Address},{h.Hostname},{(h.AvgRtt?.TotalMilliseconds.ToString() ?? "")}");
        }

        var csv = sb.ToString();
        Console.WriteLine(csv);
        SaveToFile("all", csv);
    }

    private static string Escape(string? s) => (s ?? "").Replace("\"", "\"\"");

    private static void SaveToFile(string prefix, string content)
    {
        var dir = Path.Combine(Environment.CurrentDirectory, "netstats-output");
        Directory.CreateDirectory(dir);
        var fileName = $"{prefix}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
        var path = Path.Combine(dir, fileName);
        File.WriteAllText(path, content);
        Console.WriteLine($"Saved to: {Path.GetFullPath(path)}");
    }
}
