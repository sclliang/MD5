# MD5 File Integrity Checker

A fast, native-AOT-compiled CLI tool for verifying file integrity using MD5 hashes, with a polished terminal UI powered by [Spectre.Console](https://spectreconsole.net/).

## Features

- **Baseline Creation** — On first run, recursively scans all files in the current directory and writes an `md5.txt` manifest.
- **Integrity Verification** — On subsequent runs, compares every file against the stored manifest.
- **Change Detection** — Clearly reports **Changed**, **Missing**, and **New** files with both expected and actual hashes.
- **Native AOT** — Compiled to native code with .NET 10 NativeAOT for instant startup and zero runtime dependencies.
- **Rich Terminal UI** — Figlet header, spinners, color-coded tables, and panels via Spectre.Console.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Build & Publish

```bash
# Build (debug)
dotnet build

# Publish as single-file native executable
dotnet publish -c Release -o publish
```

The compiled native executable will be at `publish\MD5.exe`.

## Usage

```bash
# Create baseline (first run in a directory)
MD5.exe

# Verify integrity (subsequent runs)
MD5.exe

# Pipe-friendly mode (no interactive prompt)
echo. | MD5.exe
```

### Exit Codes

| Code | Meaning              |
|------|----------------------|
| 0    | PASS — all files match |
| 1    | FAILED — differences found |
| 2    | Error                |

## Manifest Format (`md5.txt`)

```
<hash>  <relative/path>
<hash>  <relative/path>
...
```

Example:

```
d41d8cd98f00b204e9800998ecf8427e  data/config.json
5d41402abc4b2a76b9719d911017c592  src/main.cs
```

## License

MIT
