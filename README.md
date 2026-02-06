# netstats

CLI network diagnostics tool that can output results as plain text, JSON, CSV, or Markdown, and saves the report files into `netstats-output/`.

## Features

- Public IP + ISP/ASN info
- Geolocation lookup
- Ping + speed test
- DNS diagnostics (system DNS + quick public DNS comparison)
- Traceroute (first hops)
- Output formats: `plain`, `json`, `csv`, `markdown`

## Usage

Commands:

- `ip`
- `geo`
- `speed`
- `all` (full report)

Format selection:

- `-- plain` (default)
- `-- json` (or `--json`)
- `-- csv`
- `-- markdown` (or `-- md`)

Timeout:

- `--timeout 30000` (milliseconds)

Examples:

```bash
netstats --all --markdown --timeout 30000
netstats all -- json
netstats geo -- plain --timeout 15000
```

Output files are written to `./netstats-output/` and timestamped.

## Build

```bash
dotnet build -c Release
```

## Publish a standalone .exe (no dotnet required)

This creates a self-contained Windows executable you can run on machines without the .NET runtime installed.

From the repo root:

```bash
dotnet publish -c Release -r win-x64 --self-contained true ^
  /p:PublishSingleFile=true ^
  /p:IncludeNativeLibrariesForSelfExtract=true ^
  /p:PublishTrimmed=false
```

Result:

- `bin\Release\net8.0\win-x64\publish\netstats.exe`

Run it:

```bash
.\bin\Release\net8.0\win-x64\publish\netstats.exe --all --markdown --timeout 30000
```

Optional (make `netstats` available everywhere):

- Add `bin\Release\net8.0\win-x64\publish\` to your PATH, or copy `netstats.exe` somewhere on your PATH.

## Notes

- If the timeout is too small, some sections (like traceroute) may return partial results.
