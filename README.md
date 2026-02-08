# NetStats — Network diagnostics CLI

NetStats is a compact, dependency-free (small number of NuGet packages) command-line tool that runs a set of common network diagnostics and writes human-friendly and machine-readable reports.
It includes public IP lookup, geolocation, ping measurements, throughput (download/upload) checks, DNS diagnostics and a traceroute implementation — and can export results to JSON, CSV, Markdown or plain console output.

---

## At a glance

* **Language / Target:** .NET 8 (C#)
* **Packages used:** `Spectre.Console`, `DnsClient`, `Microsoft.Extensions.Hosting`, `Microsoft.Extensions.DependencyInjection`
* **Main features**

  * Public IP lookup (uses `ip-api.com`)
  * Geolocation lookup (uses `ip-api.com`)
  * Ping test (statistics: avg/min/max/jitter/packet loss)
  * Bandwidth test (download + upload, parallel downloads)
  * DNS diagnostics (uses system DNS servers; fallback to `8.8.8.8` and `1.1.1.1`)
  * Traceroute (TTL-based traceroute using ICMP/`Ping`)
  * Output formatters: **plain**, **json**, **csv**, **markdown**
  * Saved outputs are placed in `./netstats-output/` (timestamped files)

---

## Design / architecture

* **Separation of concerns**

  * `Services/*` implement the actual diagnostics (network operations).
  * `Output/*` format and optionally persist results (console + `netstats-output/` files).
  * `Models/*` are immutable record types used across the app.
  * `Program.cs` wires dependency injection, parses CLI args, creates progress bars (Spectre.Console) and coordinates running tasks concurrently.
* **Concurrency pattern**

  * Each selected diagnostic runs as a `Task`. Spectre.Console progress bars report progress while tasks run concurrently. Results are collected and the chosen formatter prints/saves output when each task completes (or the full report when `all` mode is used).
* **Persistence**

  * Formatters write files into `netstats-output/` using timestamps (e.g. `ip-YYYYMMDD-HHMMSS.json`).

---

## Build & install

### Prerequisites

* .NET 8 SDK — install from Microsoft (.NET 8 is the `TargetFramework` in the project).
* Internet access for speed tests, IP/geo lookups and DNS checks.

### Restore & build

```bash
git clone https://github.com/Amirreza-Jabbari/Netstats-cli.git
cd netstats
dotnet restore
dotnet build -c Release
```

### Run without publishing (useful during development)

```bash
# Run the default (plain text) output and show all checks (IP, Geo, Speed, DNS, Traceroute)
dotnet run --project ./netstats.csproj -- all

# run only IP lookup, plain output
dotnet run --project ./netstats.csproj -- ip
```

---

## CLI Usage

General pattern:

```text
dotnet run -- [MODE] [OPTIONS]
# or when running the published executable available in releases: ./netstats [MODE] [OPTIONS]
```

### Modes

* `ip` — public IP lookup (and base IP metadata).
* `geo` — geolocation for the public IP.
* `speed` — ping and download/upload tests.
* `all` — run everything (IP, Geo, Ping, Speed, DNS, Traceroute).

(You can pass `all` or run a single mode: `ip`, `geo`, `speed`.)

### Output formats

* `-- json|csv|plain|markdown` (or pass `json`, `csv`, `plain`, `markdown` as a standalone argument)
* `--json` is a convenience switch (sets JSON behavior).
* Example: `--json` or `json` (as argument) both set JSON output formatter.

### Other options

* `--timeout <ms>` — global timeout (milliseconds) applied to network operations (default: ~15000)
* `--no-color` — disable ANSI color output

### Examples

```bash
# Run everything, pretty console output
dotnet run --project ./netstats.csproj -- all

# Run speed tests only and save as JSON
dotnet run --project ./netstats.csproj -- speed --json

# Run IP lookup (plain), no colors (useful in automation)
dotnet run --project ./netstats.csproj -- ip --no-color

# Increase timeouts (20 seconds)
dotnet run --project ./netstats.csproj -- all --timeout 20000 --csv
```

---

## Where results go

* Console: output is printed in the chosen formatter (plain / markdown / json / csv).
* File: CSV / JSON / Markdown / Plain formatters save to `./netstats-output/` with timestamped filenames (for example `ip-20260131-142300.json`).
* The CSV formatter uses `.csv` extension; JSON uses `.json`, Markdown uses `.md`.

---

## Implementation notes (deep analysis of codebase)

* **IpService / GeoService**

  * Uses `ip-api.com` to fetch public IP and geolocation (`http://ip-api.com/json/...`). Responses are deserialized into `IpInfo` and `GeoInfo` models. `IpInfo` includes fields such as `Query` (IP), `Isp`, `As` (ASN), `Proxy`, `Mobile`, `Hosting`, `Country`, `RegionName`, `City`, `Lat`, `Lon` and `Timezone`.
  * Code timestamps the result with `RetrievedAt = DateTime.UtcNow`.
* **SpeedService**

  * Implements ping measurement and a parallelized download-based throughput test.
  * The code contains a `DownloadUrls` array (e.g. `https://plesk.zsaham.ir/test/10MB.zip`, `https://ipv4.download.thinkbroadband.com/10MB.zip`) used for download tests and an `UploadUrl` used for upload tests.
  * Download test uses limited parallelism and measures bytes/time to compute Mbps.
  * Ping test returns `PingResult` (AverageMs, MinMs, MaxMs, JitterMs, PacketLossPercent, Samples).
* **TracerouteService**

  * TTL-based traceroute using .NET `Ping` and varying TTL up to a configurable maximum (e.g. 30). Returns `TracerouteHop` records (Hop index, Address, Hostname, Average RTT).
  * Implementation uses `Dns.GetHostAddressesAsync` to resolve the destination before tracerouting IPv4 addresses.
* **DnsService**

  * Uses `DnsClient` to query system DNS servers and also compares timings against known public DNS servers. If system DNS servers can't be discovered it falls back to `8.8.8.8` and `1.1.1.1`.
  * `DnsResult` includes `QueriedHost`, `QueryTimeMs`, `SystemDnsServers`, and `PublicDnsComparison`.
* **Output formatters**

  * `PlainFormatter` prints pretty console output using Spectre.Console and also saves to `netstats-output`.
  * `JsonFormatter` serializes results to pretty JSON and saves to `netstats-output`.
  * `CsvFormatter` writes CSV rows and saves to `netstats-output`.
  * `MarkdownFormatter` writes a readable `.md` report and saves to `netstats-output`.

---

## Known limitations & platform notes

* **ICMP / Traceroute behaviour**

  * Firewalls and network middleboxes can drop or rate-limit ICMP/TTL-expired messages — traceroute and ping results may be partial or contain timeouts.
  * Behavior may differ between OSes and network environments.
* **Speed test servers**

  * The speed test relies on hardcoded test file URLs. If those become unreachable, the speed test will fail or degrade. Consider replacing or adding configurable endpoints in the future.
* **Public API (ip-api.com)**

  * The project uses `ip-api.com` (free tier). There are rate limits and restrictions; for heavy usage consider switching to a paid geolocation service or a local database.
* **Error handling**

  * Program catches `OperationCanceledException` and generic exceptions and exits with non-zero exit codes. Some services attempt to fall back when a step fails (e.g., DNS fallback servers).

---

## Suggestions / TODO (areas to improve)

* Make speed test endpoints configurable (CLI / config file), rather than hardcoded.
* Add unit/integration tests for service components (mock HTTP/DNS/Ping).
* Support IPv6 traceroute and IP selection.
* Provide a `--target` flag to target a specific host for `speed`, `dns`, and `trace` instead of defaulting to `google.com`.
* Add a `--output-dir` option to configure where files are saved.
* Add CI workflow and release packaging (GitHub Actions).
* Add rate-limit/backoff handling for `ip-api.com` and allow API key support for paid geolocation providers.

---

## Contributing

1. Fork the repository.
2. Create a new branch: `git checkout -b feat/your-feature`
3. Implement your change; keep the testable logic inside `Services/` and `Models/` where possible.
4. Run `dotnet format` and `dotnet build`.
5. Submit a Pull Request describing the change and motivation.

Please open an issue first for larger features or architectural changes.

---

## Troubleshooting

* **Build errors**: make sure you have .NET 8 SDK installed (`dotnet --version` should show `8.x.x`).
* **Speed tests failing**: check network connectivity and whether the hardcoded test URLs are reachable (`curl`/`wget` from the same host).
* **Traceroute/ping timeouts**: some networks block ICMP. Try a different network or run as admin/root if your environment requires that (rare for .NET `Ping`).
* **Output files not found**: by default files go to `./netstats-output/` in your current working directory.

---

## License

This project is provided under the **MIT License**. See [MIT License](LICENSE)for details.
