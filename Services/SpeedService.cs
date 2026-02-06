namespace NetStats.Services;

using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using NetStats.Models;
using NetStats.Services.Interfaces;
using Spectre.Console;

public class SpeedService : ISpeedService
{
    private readonly HttpClient _http;

    private const int DownloadParallelism = 4;
    private static readonly TimeSpan DownloadDuration = TimeSpan.FromSeconds(8);
    private const int UploadParallelism = 4;
    private static readonly TimeSpan UploadDuration = TimeSpan.FromSeconds(12);

    private static readonly string[] DownloadUrls = new[]
    {
        "https://plesk.zsaham.ir/test/10MB.zip",
        "https://ipv4.download.thinkbroadband.com/10MB.zip"
    };

    private static readonly string UploadUrl = "https://plesk.zsaham.ir/test/10MB.zip";

    public SpeedService(HttpClient? http = null)
    {
        _http = http ?? new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("netstats", "1.0"));
    }

    public async Task<PingResult> PingTestAsync(string host, TimeSpan duration, CancellationToken ct = default)
    {
        var ping = new Ping();
        var sw = Stopwatch.StartNew();
        var samples = new List<long>();
        int sent = 0, received = 0;

        while (sw.Elapsed < duration && !ct.IsCancellationRequested)
        {
            try
            {
                sent++;
                var reply = await ping.SendPingAsync(host, 2000);
                if (reply.Status == IPStatus.Success)
                {
                    samples.Add(reply.RoundtripTime);
                    received++;
                }
                else
                {
                    // timeouts or other statuses are treated as lost
                }
            }
            catch
            {
                // ignore per-ping exceptions
            }

            await Task.Delay(200, ct);
        }

        double average = samples.Any() ? samples.Average() : 0;
        double min = samples.Any() ? samples.Min() : 0;
        double max = samples.Any() ? samples.Max() : 0;
        double jitter = samples.Any() ? StdDev(samples.Select(x => (double)x)) : 0;
        double loss = sent > 0 ? ((double)(sent - received) / sent) * 100.0 : 0;

        return new PingResult
        {
            AverageMs = average,
            MinMs = min,
            MaxMs = max,
            JitterMs = jitter,
            PacketLossPercent = loss,
            Samples = samples.Count
        };
    }

    private static double StdDev(IEnumerable<double> values)
    {
        var arr = values.ToArray();
        if (!arr.Any()) return 0;
        var avg = arr.Average();
        var sum = arr.Select(v => (v - avg) * (v - avg)).Sum();
        return Math.Sqrt(sum / arr.Length);
    }

    public async Task<SpeedResult> RunSpeedTestAsync(CancellationToken ct = default)
    {
        string? selected = null;
        foreach (var url in DownloadUrls)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Head, url);
                using var res = await _http.SendAsync(req, ct);
                if (res.IsSuccessStatusCode)
                {
                    selected = url;
                    break;
                }
            }
            catch
            {
                // ignore unreachable
            }
        }

        if (selected == null)
        {
            AnsiConsole.MarkupLine("[yellow]No download test endpoints are reachable. Returning zeroed speed results.[/]");
            return new SpeedResult
            {
                DownloadMbps = 0,
                UploadMbps = 0,
                Duration = TimeSpan.Zero,
                ServerUsed = "(no endpoints reachable)"
            };
        }

        var bytesDownloaded = 0L;
        var sw = Stopwatch.StartNew();

        var tasks = Enumerable.Range(0, DownloadParallelism).Select(async _ =>
        {
            var rnd = new Random();
            while (sw.Elapsed < DownloadDuration && !ct.IsCancellationRequested)
            {
                try
                {
                    long start = rnd.Next(0, 10_000_000);
                    var req = new HttpRequestMessage(HttpMethod.Get, selected);
                    req.Headers.Range = new RangeHeaderValue(start, start + 131071); // 128KB
                    using var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
                    if (!res.IsSuccessStatusCode) continue;
                    using var stream = await res.Content.ReadAsStreamAsync(ct);
                    var buffer = ArrayPool<byte>.Shared.Rent(81920);
                    try
                    {
                        int read;
                        while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0 && sw.Elapsed < DownloadDuration)
                        {
                            Interlocked.Add(ref bytesDownloaded, read);
                        }
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }
                }
                catch
                {
                    // ignore per-chunk exceptions
                }
            }
        }).ToArray();

        await Task.WhenAll(tasks);
        sw.Stop();
        double downloadMbps = bytesDownloaded * 8.0 / sw.Elapsed.TotalSeconds / 1_000_000.0;

        // Upload test (POST random chunks)
        long bytesUploaded = 0L;
        var uplSw = Stopwatch.StartNew();
        var uploadTasks = Enumerable.Range(0, UploadParallelism).Select(async _ =>
        {
            var rnd = new Random();
            while (uplSw.Elapsed < UploadDuration && !ct.IsCancellationRequested)
            {
                try
                {
                    var payload = new byte[256 * 1024]; // 256KB
                    rnd.NextBytes(payload);
                    using var content = new ByteArrayContent(payload);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    var response = await _http.PutAsync(UploadUrl, content, ct);
                    response.Dispose();
                    Interlocked.Add(ref bytesUploaded, payload.Length);
                }
                catch
                {
                    // ignore
                }
            }
        }).ToArray();

        await Task.WhenAll(uploadTasks);
        uplSw.Stop();
        double uploadMbps = bytesUploaded * 8.0 / Math.Max(1, uplSw.Elapsed.TotalSeconds) / 1_000_000.0;

        return new SpeedResult
        {
            DownloadMbps = Math.Round(downloadMbps, 2),
            UploadMbps = Math.Round(uploadMbps, 2),
            Duration = sw.Elapsed,
            ServerUsed = selected
        };
    }
}
