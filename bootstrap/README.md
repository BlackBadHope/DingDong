# Official dependencies and complete installation

Run the repository root `Install-DingDong.cmd` for the complete installation.
The supported target is Windows x64 with licensed Terraria, tModLoader 2026.8.3.0
and an online Steam client. No .NET SDK is required on the player computer.

- `Install-Core.ps1`: signed-plan and ZIP validation, protected paths,
  separate runtime, transactional profile installation and rollback.
- `Prepare-PublicDependencies.ps1`: exact public preset, official downloads,
  verified local package reuse and staging.
- `Ensure-OfficialDependenciesForLaunch.ps1`: exact installed-profile validation.
- `Build-OfficialOre.ps1` and `SourceBuild/`: deterministic Release build of
  OreExcavator 0.8.9 from the author's pinned commit. Uses compiler assemblies
  already present in the official tModLoader. Debug-only TUnit is not packaged.
- `public-dependencies.json`: 65 external dependencies: 63 Workshop packages,
  StructureHelper from the author's release and locally built OreExcavator.
- `public-owned-mod.json`: exact project-owned DingDong package identity.

The root installer checks the signed GitHub release, Steam entitlement, all
external package hashes and the private runtime before replacing managed Mods.
A repeated run verifies an existing installation and repairs changed files.
Players, Worlds, maps, existing configuration and keybindings are preserved.
See the root README for parameters and rollback.

## Read-only historical resolver

`Resolve-OfficialDependencies.ps1` remains a separate read-only inventory tool
for `manifests/r22-reference.json`. It does not download, copy, enable or launch
anything. Its result is dependency availability, not complete installation.

```powershell
.\bootstrap\Resolve-OfficialDependencies.ps1
```

Every package is matched by internal mod name, size and SHA-256. Newer or changed
packages never silently replace the pinned version. The complete installer can
request missing Workshop items through the official Steam API and requires
actual download-completion callbacks before validating the resulting bytes.
Author packages and sources are downloaded directly on the player's computer;
they are not mirrored by this repository.
