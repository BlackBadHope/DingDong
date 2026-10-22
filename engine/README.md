# Local engine reproduction

This directory contains our patcher source and a hash contract. It contains no tModLoader/Terraria runtime, assets, native Steam API, decompiled game source, or Workshop content.

Install the official tModLoader build `2026.08.3.0` through Steam. Its unmodified `tModLoader.dll` must match `patch-contract.json`. An existing legacy DingDong installation may already contain a modified engine; verify the official files through Steam before using it as input. Do not patch the Steam installation in place.

Build with .NET SDK `9.0.315` and Mono.Cecil `0.11.6` obtained from the official NuGet source in `NuGet.Config`:

```powershell
dotnet build EnginePatch.csproj -c Release --configfile NuGet.Config
dotnet bin/Release/net8.0/EnginePatch.dll '<official-tML>/tModLoader.dll' '<private-runtime>/tModLoader.dll'
```

The output path and adjacent `.patch-verification.json` must not exist. The patcher compares all normalized method bodies, metadata and resources with the input, preserves the original MaxStack values and requires the exact output SHA256. A different input, a second matching literal, a changed assembly contract or a nonmatching output fails closed. It never starts the game or modifies the input.

The sole semantic engine change passes the private `DingDongWorkshop` directory to the Host & Play child server. The working directory must be the private runtime, and that directory must already exist. Runtime dependencies and assets are supplied locally from the user's official installation by the separately reviewed bootstrap; never place them in a public release artifact.

The local upstream baseline was preserved with SHA256 `FCC6…DA0A5`; its recorded Steam provenance is documented in the audit. A fresh independent Steam download was not performed by this source-only preparation. Rebuilding all of upstream tML from source is an alternative requiring its official Terraria-dependent setup, but byte-identical output from that different build route is not promised.

Upstream tModLoader repository source is MIT licensed: [pinned LICENSE](https://github.com/tModLoader/tModLoader/blob/c9545cf27d56c456321e1896274c02adf67d0140/LICENSE). This does not establish permission to mirror the proprietary Terraria base or third-party Workshop assets. Mono.Cecil is an MIT dependency: [official repository](https://github.com/jbevain/cecil), [official NuGet package](https://www.nuget.org/packages/Mono.Cecil/0.11.6). Preserve applicable notices if distributing dependency binaries or substantial upstream source. The patcher source here is our own work; the parent repository owns the final licence and notice files.
