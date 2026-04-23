# Package sizing notes

Measured on 2026-03-23 on Linux (`linux-x64`) with Release publishes and debug symbols disabled.

Commands used:

```bash
dotnet publish TibiaHuntMaster.App/TibiaHuntMaster.App.csproj -c Release -r linux-x64 --self-contained false -p:DebugType=None -p:DebugSymbols=false -o /tmp/thm-publish/framework-dependent
dotnet publish TibiaHuntMaster.App/TibiaHuntMaster.App.csproj -c Release -r linux-x64 --self-contained false -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false -o /tmp/thm-publish/single-file
dotnet publish TibiaHuntMaster.App/TibiaHuntMaster.App.csproj -c Release -r linux-x64 --self-contained false -p:PublishTrimmed=true -p:DebugType=None -p:DebugSymbols=false -o /tmp/thm-publish/trimmed
```

Results:

| Strategy | Size (bytes) | Size (approx.) | Notes |
| --- | ---: | ---: | --- |
| Framework-dependent | 50,698,194 | 49 MB | Smallest safe measured output in this run |
| Single-file | 124,176,643 | 119 MB | Larger than framework-dependent on Linux because managed content is bundled into one executable |
| Trimmed | 72,763,460 | 70 MB | Smaller than single-file, but not verified safe |

## Decision

Current recommendation:
- prefer framework-dependent publishes as the default slim distribution baseline
- use single-file only when simpler distribution is more important than package size
- do not enable trimming by default yet

## Why trimming is not adopted yet

The trimmed publish produced extensive ILLink warnings in reflection-heavy paths, including:
- Avalonia view resolution and XAML-related reflection
- EF Core runtime model access
- JSON serialization / metadata-driven code paths

That means trimming is measured, but not verified safe enough for default packaging.

## Follow-up if trimming is revisited later

Before enabling trimming by default:
- run startup smoke tests on Windows, Linux, and macOS
- verify navigation, localization, database init, imports, and map rendering
- resolve or intentionally annotate the trim warnings in the affected reflection paths
