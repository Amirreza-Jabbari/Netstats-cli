using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetStats.Output;
using NetStats.Services;
using NetStats.Services.Interfaces;
using NetStats.Utils;
using Spectre.Console;
using System.Net;
using NetStats.Models;

namespace NetStats;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        using IHost host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton<IIPService, IpService>();
                services.AddSingleton<IGeoService, GeoService>();
                services.AddSingleton<ISpeedService, SpeedService>();
                services.AddSingleton<IDnsService, DnsService>();
                services.AddSingleton<ITracerouteService, TracerouteService>();

                services.AddSingleton<PlainFormatter>();
                services.AddSingleton<JsonFormatter>();
                services.AddSingleton<CsvFormatter>();
                services.AddSingleton<MarkdownFormatter>();

                services.AddSingleton<IOutputFormatter>(sp => sp.GetRequiredService<PlainFormatter>());
            })
            .Build();

        // Defaults
        var format = "plain";
        var jsonSwitch = false;
        var timeout = 15000;
        var noColor = false;
        string? mode = null;

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (arg.StartsWith("--format="))
            {
                format = arg.Substring("--format=".Length);
            }
            else if (arg == "--format" && i + 1 < args.Length)
            {
                format = args[++i];
            }
            else if (arg == "--json")
            {
                jsonSwitch = true;
            }
            else if (arg.StartsWith("--timeout=") && int.TryParse(arg.Substring("--timeout=".Length), out var t1))
            {
                timeout = t1;
            }
            else if (arg == "--timeout" && i + 1 < args.Length && int.TryParse(args[i + 1], out var t2))
            {
                timeout = t2;
                i++;
            }
            else if (arg == "--no-color")
            {
                noColor = true;
            }
            else if (arg == "--" && i + 1 < args.Length)
            {
                var next = args[++i].ToLowerInvariant();
                if (next is "json" or "csv" or "plain" or "markdown" or "md")
                {
                    format = next switch
                    {
                        "md" => "md",
                        _ => next
                    };
                    if (next == "json") jsonSwitch = true;
                }
                else if (next is "ip" or "geo" or "speed" or "all")
                {
                    mode = next;
                }
            }
            else if (arg.Equals("json", StringComparison.OrdinalIgnoreCase) ||
                     arg.Equals("csv", StringComparison.OrdinalIgnoreCase) ||
                     arg.Equals("plain", StringComparison.OrdinalIgnoreCase) ||
                     arg.Equals("markdown", StringComparison.OrdinalIgnoreCase) ||
                     arg.Equals("md", StringComparison.OrdinalIgnoreCase))
            {
                var f = arg.ToLowerInvariant();
                format = f == "md" ? "md" : f;
                if (format == "json") jsonSwitch = true;
            }
            else if (mode is null && (arg is "ip" or "geo" or "speed" or "all" or "--all"))
            {
                mode = arg.TrimStart('-');
            }
        }

        if (mode is null)
        {
            AnsiConsole.MarkupLine("[green]netstats - run 'netstats --help' for usage (commands: ip, geo, speed, all)[/]");
            return 0;
        }

        await RunWithCommonOptionsAsync(host, mode, format, jsonSwitch, timeout, noColor);
        return Environment.ExitCode;
    }

    private static async Task RunWithCommonOptionsAsync(
        IHost host,
        string mode,
        string format,
        bool jsonSwitch,
        int timeout,
        bool noColor)
    {
        if (jsonSwitch)
        {
            format = "json";
        }

        if (noColor)
        {
            ConsoleStyling.DisableColors();
        }

        var sp = host.Services;
        IOutputFormatter formatter = format.ToLowerInvariant() switch
        {
            "json" => sp.GetRequiredService<JsonFormatter>(),
            "csv" => sp.GetRequiredService<CsvFormatter>(),
            "md" or "markdown" => sp.GetRequiredService<MarkdownFormatter>(),
            _ => sp.GetRequiredService<PlainFormatter>()
        };

        var ipSvc = sp.GetRequiredService<IIPService>();
        var geoSvc = sp.GetRequiredService<IGeoService>();
        var speedSvc = sp.GetRequiredService<ISpeedService>();
        var dnsSvc = sp.GetRequiredService<IDnsService>();
        var tracerouteSvc = sp.GetRequiredService<ITracerouteService>();

        var cts = new CancellationTokenSource(timeout);

        try
        {
            IpInfo? ip = null;
            GeoInfo? geo = null;
            SpeedResult? speed = null;
            PingResult ping = new()
            {
                AverageMs = 0,
                MinMs = 0,
                MaxMs = 0,
                JitterMs = 0,
                PacketLossPercent = 0,
                Samples = 0
            };
            DnsResult? dnsRes = null;
            IEnumerable<TracerouteHop> trace = Array.Empty<TracerouteHop>();

            await AnsiConsole.Progress()
                .AutoClear(false)
                .HideCompleted(false)
                .StartAsync(async progress =>
                {
                    var ipTask = (mode is "ip" or "geo" or "all")
                        ? progress.AddTask("[green]Resolving public IP[/]", maxValue: 100)
                        : null;

                    var geoTask = (mode is "geo" or "all")
                        ? progress.AddTask("[green]Resolving geolocation[/]", maxValue: 100)
                        : null;

                    var pingTask = (mode is "speed" or "all")
                        ? progress.AddTask("[green]Measuring ping[/]", maxValue: 100)
                        : null;

                    var speedTask = (mode is "speed" or "all")
                        ? progress.AddTask("[green]Running speed test[/]", maxValue: 100)
                        : null;

                    var dnsTask = mode == "all"
                        ? progress.AddTask("[green]Checking DNS performance[/]", maxValue: 100)
                        : null;

                    var traceTask = mode == "all"
                        ? progress.AddTask("[green]Running traceroute[/]", maxValue: 100)
                        : null;

                    var pending = new List<(string Name, Task Work, Spectre.Console.ProgressTask Bar, Func<Task> Store)>();

                    Task<IpInfo>? ipWork = null;
                    if (ipTask != null)
                    {
                        ipWork = ipSvc.GetPublicIpInfoAsync(cts.Token);
                        pending.Add(("IP", ipWork, ipTask, async () => ip = await ipWork));
                    }

                    if (geoTask != null)
                    {
                        ipWork ??= ipSvc.GetPublicIpInfoAsync(cts.Token);
                        var geoWork = Task.Run(async () =>
                        {
                            var ipRes = await ipWork;
                            return await geoSvc.GetGeoForIpAsync(ipRes.Query, cts.Token);
                        }, cts.Token);
                        pending.Add(("Geolocation", geoWork, geoTask, async () => geo = await geoWork));
                    }

                    if (pingTask != null)
                    {
                        var pingWork = speedSvc.PingTestAsync("google.com", TimeSpan.FromSeconds(5), cts.Token);
                        pending.Add(("Ping", pingWork, pingTask, async () => ping = await pingWork));
                    }

                    if (speedTask != null)
                    {
                        var speedWork = speedSvc.RunSpeedTestAsync(cts.Token);
                        pending.Add(("Speed test", speedWork, speedTask, async () => speed = await speedWork));
                    }

                    if (dnsTask != null)
                    {
                        var dnsWork = dnsSvc.RunDnsDiagnosticsAsync("google.com", cts.Token);
                        pending.Add(("DNS", dnsWork, dnsTask, async () => dnsRes = await dnsWork));
                    }

                    if (traceTask != null)
                    {
                        var traceWork = tracerouteSvc.TracerouteAsync("google.com", cts.Token);
                        pending.Add(("Traceroute", traceWork, traceTask, async () => trace = await traceWork));
                    }

                    while (pending.Count > 0)
                    {
                        var completed = await Task.WhenAny(pending.Select(p => p.Work));
                        var index = pending.FindIndex(p => p.Work == completed);
                        var item = pending[index];
                        pending.RemoveAt(index);

                        try
                        {
                            await item.Store();
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            ConsoleStyling.WriteError($"{item.Name} failed: {ex.Message}");
                        }

                        item.Bar.Increment(100);
                    }
                });

            if (mode is "ip" or "geo" or "all")
                ip ??= new IpInfo { Query = "unknown", Isp = "unknown", As = "unknown" };
            if (mode is "geo" or "all")
                geo ??= new GeoInfo { Ip = ip?.Query ?? "unknown" };
            if (mode is "speed" or "all")
                speed ??= new SpeedResult { DownloadMbps = 0, UploadMbps = 0, Duration = TimeSpan.Zero, ServerUsed = "-" };
            if (mode is "all")
                dnsRes ??= new DnsResult { QueriedHost = "google.com", QueryTimeMs = double.NaN };

            if (mode is "ip")
            {
                formatter.WriteIp(ip!);
            }
            else if (mode is "geo")
            {
                formatter.WriteGeo(geo!, ip);
            }
            else if (mode is "speed")
            {
                formatter.WriteSpeed(speed!, ping);
            }
            else if (mode is "all")
            {
                formatter.WriteFullReport(ip!, geo!, speed!, ping, dnsRes!, trace);
            }
        }
        catch (OperationCanceledException)
        {
            ConsoleStyling.WriteError("Operation timed out or cancelled.");
            Environment.ExitCode = 2;
        }
        catch (Exception ex)
        {
            ConsoleStyling.WriteError($"Unhandled error: {ex.Message}");
            Environment.ExitCode = 1;
        }
    }
}
