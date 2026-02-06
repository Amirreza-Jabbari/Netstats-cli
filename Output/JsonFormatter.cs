using System.Text.Json;
using System.Text.Json.Serialization;
using NetStats.Models;

namespace NetStats.Output;

public class JsonFormatter : IOutputFormatter
{
    private readonly JsonSerializerOptions _opts = new()
    {
        WriteIndented = true,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
    };

    public void WriteIp(IpInfo ip)
    {
        var json = JsonSerializer.Serialize(ip, _opts);
        Console.WriteLine(json);
        SaveToFile("ip", json);
    }

    public void WriteGeo(GeoInfo geo, IpInfo? ip = null)
    {
        var json = JsonSerializer.Serialize(new { geo, ip }, _opts);
        Console.WriteLine(json);
        SaveToFile("geo", json);
    }

    public void WriteSpeed(SpeedResult speed, PingResult ping)
    {
        var json = JsonSerializer.Serialize(new { speed, ping }, _opts);
        Console.WriteLine(json);
        SaveToFile("speed", json);
    }

    public void WriteFullReport(IpInfo ip, GeoInfo geo, SpeedResult speed, PingResult ping, DnsResult dns, IEnumerable<TracerouteHop> trace)
    {
        var r = new Report
        {
            Ip = ip,
            Geo = geo,
            Speed = speed,
            Ping = ping,
            Dns = dns,
            Traceroute = trace
        };
        var json = JsonSerializer.Serialize(r, _opts);
        Console.WriteLine(json);
        SaveToFile("all", json);
    }

    private static void SaveToFile(string prefix, string content)
    {
        var dir = Path.Combine(Environment.CurrentDirectory, "netstats-output");
        Directory.CreateDirectory(dir);
        var fileName = $"{prefix}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
        var path = Path.Combine(dir, fileName);
        File.WriteAllText(path, content);
        Console.WriteLine($"Saved to: {Path.GetFullPath(path)}");
    }
}
