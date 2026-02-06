using NetStats.Models;

namespace NetStats.Output;

public interface IOutputFormatter
{
    void WriteIp(IpInfo ip);
    void WriteGeo(GeoInfo geo, IpInfo? ip = null);
    void WriteSpeed(SpeedResult speed, PingResult ping);
    void WriteFullReport(IpInfo ip, GeoInfo geo, SpeedResult speed, PingResult ping, DnsResult dns, IEnumerable<TracerouteHop> trace);
}
