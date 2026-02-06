using NetStats.Models;
using Spectre.Console;

namespace NetStats.Output;

public class PlainFormatter : IOutputFormatter
{
    private static string FormatMs(double ms)
    {
        if (double.IsNaN(ms)) return "-";
        if (double.IsInfinity(ms)) return "timeout";
        return $"{ms:N0} ms";
    }

    public void WriteIp(IpInfo ip)
    {
        AnsiConsole.MarkupLine("[green]IP Information[/]");
        AnsiConsole.MarkupLine($"[white]IP:[/] {ip.Query}");
        AnsiConsole.MarkupLine($"[white]ISP:[/] {ip.Isp}  [white]ASN:[/] {ip.As}");
        AnsiConsole.MarkupLine($"[white]Proxy:[/] {ip.Proxy}  [white]Mobile:[/] {ip.Mobile}");

        var text =
            "IP Information\n" +
            $"IP: {ip.Query}\n" +
            $"ISP: {ip.Isp}  ASN: {ip.As}\n" +
            $"Proxy: {ip.Proxy}  Mobile: {ip.Mobile}\n";
        SaveToFile("ip", text);
    }

    public void WriteGeo(GeoInfo geo, IpInfo? ip = null)
    {
        AnsiConsole.MarkupLine("[green]Geolocation[/]");
        AnsiConsole.MarkupLine($"[white]IP:[/] {geo.Ip}");
        AnsiConsole.MarkupLine($"[white]Country:[/] {geo.Country}");
        AnsiConsole.MarkupLine($"[white]Region:[/] {geo.Region}");
        AnsiConsole.MarkupLine($"[white]City:[/] {geo.City}");
        AnsiConsole.MarkupLine($"[white]Lat,Lon:[/] {geo.Latitude}, {geo.Longitude}");
        AnsiConsole.MarkupLine($"[white]Timezone:[/] {geo.Timezone}");

        var text =
            "Geolocation\n" +
            $"IP: {geo.Ip}\n" +
            $"Country: {geo.Country}\n" +
            $"Region: {geo.Region}\n" +
            $"City: {geo.City}\n" +
            $"Lat,Lon: {geo.Latitude}, {geo.Longitude}\n" +
            $"Timezone: {geo.Timezone}\n";
        SaveToFile("geo", text);
    }

    public void WriteSpeed(SpeedResult speed, PingResult ping)
    {
        AnsiConsole.MarkupLine("[green]Speed Test[/]");
        AnsiConsole.MarkupLine($"[white]Download:[/] {speed.DownloadMbps} Mbps");
        AnsiConsole.MarkupLine($"[white]Upload:[/] {speed.UploadMbps} Mbps");
        AnsiConsole.MarkupLine($"[white]Server:[/] {speed.ServerUsed}");
        AnsiConsole.MarkupLine("[green]Ping[/]");
        AnsiConsole.MarkupLine($"[white]Avg:[/] {ping.AverageMs} ms  [white]Jitter:[/] {ping.JitterMs} ms  [white]Loss:[/] {ping.PacketLossPercent}%");

        var text =
            "Speed Test\n" +
            $"Download: {speed.DownloadMbps} Mbps\n" +
            $"Upload: {speed.UploadMbps} Mbps\n" +
            $"Server: {speed.ServerUsed}\n" +
            "Ping\n" +
            $"Avg: {ping.AverageMs} ms  Jitter: {ping.JitterMs} ms  Loss: {ping.PacketLossPercent}%\n";
        SaveToFile("speed", text);
    }

    public void WriteFullReport(IpInfo ip, GeoInfo geo, SpeedResult speed, PingResult ping, DnsResult dns, IEnumerable<TracerouteHop> trace)
    {
        AnsiConsole.MarkupLine("[green]IP Information[/]");
        AnsiConsole.MarkupLine($"[white]IP:[/] {ip.Query}");
        AnsiConsole.MarkupLine($"[white]ISP:[/] {ip.Isp}  [white]ASN:[/] {ip.As}");
        AnsiConsole.MarkupLine($"[white]Proxy:[/] {ip.Proxy}  [white]Mobile:[/] {ip.Mobile}");

        AnsiConsole.WriteLine("");
        AnsiConsole.MarkupLine("[green]Geolocation[/]");
        AnsiConsole.MarkupLine($"[white]IP:[/] {geo.Ip}");
        AnsiConsole.MarkupLine($"[white]Country:[/] {geo.Country}");
        AnsiConsole.MarkupLine($"[white]Region:[/] {geo.Region}");
        AnsiConsole.MarkupLine($"[white]City:[/] {geo.City}");
        AnsiConsole.MarkupLine($"[white]Lat,Lon:[/] {geo.Latitude}, {geo.Longitude}");
        AnsiConsole.MarkupLine($"[white]Timezone:[/] {geo.Timezone}");

        AnsiConsole.WriteLine("");
        AnsiConsole.MarkupLine("[green]Speed Test[/]");
        AnsiConsole.MarkupLine($"[white]Download:[/] {speed.DownloadMbps} Mbps");
        AnsiConsole.MarkupLine($"[white]Upload:[/] {speed.UploadMbps} Mbps");
        AnsiConsole.MarkupLine($"[white]Server:[/] {speed.ServerUsed}");
        AnsiConsole.MarkupLine("[green]Ping[/]");
        AnsiConsole.MarkupLine($"[white]Avg:[/] {ping.AverageMs} ms  [white]Jitter:[/] {ping.JitterMs} ms  [white]Loss:[/] {ping.PacketLossPercent}%");

        AnsiConsole.WriteLine("");
        AnsiConsole.MarkupLine("[green]DNS[/]");
        AnsiConsole.MarkupLine($"[white]Queried:[/] {dns.QueriedHost}  [white]Time:[/] {FormatMs(dns.QueryTimeMs)}");
        AnsiConsole.MarkupLine($"[white]System DNS:[/] {string.Join(", ", dns.SystemDnsServers)}");
        AnsiConsole.MarkupLine("[white]Public DNS comparison:[/]");
        foreach (var p in dns.PublicDnsComparison)
        {
            var t = FormatMs(p.TimeMs);
            AnsiConsole.MarkupLine($" - {p.Server}: {t}");
        }

        AnsiConsole.WriteLine("");
        AnsiConsole.MarkupLine("[green]Traceroute (first hops)[/]");
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("IP Information");
        sb.AppendLine($"IP: {ip.Query}");
        sb.AppendLine($"ISP: {ip.Isp}  ASN: {ip.As}");
        sb.AppendLine($"Proxy: {ip.Proxy}  Mobile: {ip.Mobile}");
        sb.AppendLine();

        sb.AppendLine("Geolocation");
        sb.AppendLine($"IP: {geo.Ip}");
        sb.AppendLine($"Country: {geo.Country}");
        sb.AppendLine($"Region: {geo.Region}");
        sb.AppendLine($"City: {geo.City}");
        sb.AppendLine($"Lat,Lon: {geo.Latitude}, {geo.Longitude}");
        sb.AppendLine($"Timezone: {geo.Timezone}");
        sb.AppendLine();

        sb.AppendLine("Speed Test");
        sb.AppendLine($"Download: {speed.DownloadMbps} Mbps");
        sb.AppendLine($"Upload: {speed.UploadMbps} Mbps");
        sb.AppendLine($"Server: {speed.ServerUsed}");
        sb.AppendLine("Ping");
        sb.AppendLine($"Avg: {ping.AverageMs} ms  Jitter: {ping.JitterMs} ms  Loss: {ping.PacketLossPercent}%");
        sb.AppendLine();

        sb.AppendLine("DNS");
        sb.AppendLine($"Queried: {dns.QueriedHost}  Time: {FormatMs(dns.QueryTimeMs)}");
        sb.AppendLine($"System DNS: {string.Join(", ", dns.SystemDnsServers)}");
        sb.AppendLine("Public DNS comparison:");
        foreach (var p in dns.PublicDnsComparison)
        {
            sb.AppendLine($" - {p.Server}: {FormatMs(p.TimeMs)}");
        }
        sb.AppendLine();

        sb.AppendLine("Traceroute (first hops)");
        var anyHop = false;
        foreach (var hop in trace.Take(12))
        {
            anyHop = true;
            var rtt = hop.AvgRtt.HasValue ? $"{hop.AvgRtt.Value.TotalMilliseconds:N0} ms" : "-";
            var line = $"{hop.Hop} {hop.Address ?? "-"} ({hop.Hostname ?? "-"}) - {rtt}";
            AnsiConsole.WriteLine(line);
            sb.AppendLine(line);
        }
        if (!anyHop)
        {
            var line = "- - (-) - -";
            AnsiConsole.WriteLine(line);
            sb.AppendLine(line);
        }

        SaveToFile("all", sb.ToString());
    }

    private static void SaveToFile(string prefix, string content)
    {
        var dir = Path.Combine(Environment.CurrentDirectory, "netstats-output");
        Directory.CreateDirectory(dir);
        var fileName = $"{prefix}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.txt";
        var path = Path.Combine(dir, fileName);
        File.WriteAllText(path, content);
        AnsiConsole.MarkupLine($"[grey]Saved to: {Path.GetFullPath(path)}[/]");
    }
}
