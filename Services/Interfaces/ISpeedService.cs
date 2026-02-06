namespace NetStats.Services.Interfaces;

using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using NetStats.Models;
using NetStats.Services.Interfaces;
using Spectre.Console;

public interface ISpeedService
{
    Task<PingResult> PingTestAsync(string host, TimeSpan duration, CancellationToken ct = default);
    Task<SpeedResult> RunSpeedTestAsync(CancellationToken ct = default);
}
