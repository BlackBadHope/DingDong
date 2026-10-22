# Official dependency resolver (source-only)

`Resolve-OfficialDependencies.ps1` reads the historical `manifests/r22-reference.json`
inventory, excludes the old DingDong binary, and looks only in installed Steam
Workshop item folders. It never downloads, copies, enables, rebuilds or launches
anything. It does not change the launcher, updater, game, profile or saves.

```powershell
.\bootstrap\Resolve-OfficialDependencies.ps1
# Optional report; its parent folder must already exist:
.\bootstrap\Resolve-OfficialDependencies.ps1 -ReportPath .\dependency-check.json
# Explicitly open at most five missing/incompatible official Workshop pages:
.\bootstrap\Resolve-OfficialDependencies.ps1 -OpenWorkshopPages -MaxOpenPages 5
```

The default run opens no links and makes no network requests. The explicit
link flag opens HTTPS pages in the user's browser; it never subscribes or calls
Steam install/update commands. The user controls subscriptions/downloads in
Steam. Opening a page is not proof of installing its item.

Steam discovery uses the registered client root (or explicit `-SteamPath`),
`steam.exe`, its `steamapps/libraryfolders.vdf`, and the official app manifests
for Terraria 105600 and tModLoader 1281930. No drive-wide search or profile/cache
fallback is used. Paths containing junctions/symlinks are rejected. These are
installation-layout checks, not proof of account ownership or cryptographic
authentication of every installed Steam executable.

For each known Workshop ID, the resolver reads bounded TMOD header metadata and
matches the **internal mod name**, not the package filename or newest folder.
It checks exact byte count and full SHA256 against the original reference.
Files of a different size are already incompatible and are not fully hashed;
the report marks that boundary. No package payload is extracted or executed.

Possible results are `AVAILABLE_EXACT`, `MISSING_OFFICIAL`,
`INCOMPATIBLE_OFFICIAL`, and `UNRESOLVED_OFFICIAL_SOURCE`. A newer Workshop
version, historical old version, modified local reference, wrong internal name,
invalid header or different bytes never silently replace the pinned package.
Every blocked row includes its action and official link when one is known.
Exit code 2 means blocked prerequisites, 1 means a script/configuration error,
and 0 means only that the referenced dependencies were resolved. The report
always keeps `completeCleanInstall=false`.

`official-sources.json` supplies two source-only hints absent from the original
reference: [Ore Excavator 2565639705](https://steamcommunity.com/sharedfiles/filedetails/?id=2565639705)
([author repository](https://github.com/CrazyContraption/OreExcavator)) and
[Structure Helper 2790924965](https://steamcommunity.com/sharedfiles/filedetails/?id=2790924965)
([author repository](https://github.com/ScalarVector1/StructureHelper)). These IDs
do not override any expected hash. Exact reference package identity is still
checked locally. Steam's direct page reads returned rate limits during this
lookup; the item/author associations were visible in indexed primary pages.
No exact-name primary public source was identified for `PackWorldgenFix` in the
bounded lookup. It remains blocked; no similar mod or guessed ID is substituted.

The metadata reader follows the documented field order of upstream
[tModLoader TmodFile](https://github.com/tModLoader/tModLoader/blob/stable/patches/tModLoader/Terraria/ModLoader/Core/TmodFile.cs).
It is an independent read-only header reader and does not load upstream game
assemblies or copy their implementation.

Offline checks: `powershell.exe -NoProfile -File bootstrap/tests/Test-OfficialResolver.ps1`.
Fixtures use synthetic header-only files in an ignored `_work` folder. Actual
Steam launch, subscription, download and complete clean installation are not
part of those tests.
