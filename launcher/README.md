# Steam-checked client launcher candidate

Build `SteamLicenseGate.csproj` in Release mode. The helper targets .NET 8 and uses the official tML runtime when launched. No Steamworks or game binaries are included in this directory.

```powershell
dotnet build SteamLicenseGate.csproj -c Release --configfile NuGet.Config
dotnet bin/Release/net8.0/SteamLicenseGate.dll --self-test
.\Launch-DingDong.cmd -OfficialTmlPath '<Steam official tML>' -RuntimePath '<private prepared runtime>' -ProfilePath '<DingDong profile>'
```

The mock self-test does not load the Steam API, connect to Steam, inspect accounts, write a profile or launch a game. A real check loads the pinned installed `Steamworks.NET.dll` and `steam_api64.dll`, requires a running Steam client in an online session and the real tML App ID `1281930`, then asks `BIsSubscribedApp(105600)` and `BIsAppInstalled(105600)` for Terraria. It also requires installed tML and compares Steam's returned tML directory with the supplied directory. Missing Steam, an unsupported API build, a conflicting app context or an unverified licence aborts launch. No API key, password, login cookie or Steam ID is saved or printed.

This proves an active Steam entitlement according to the installed Steam API. It is not a payment receipt and cannot distinguish a personal purchase from every legitimate shared or granted entitlement in the tML app context. `GetAppOwner` and family-sharing queries on the free current tML app must not be presented as Terraria ownership proof. If policy requires the active account's personal permanent purchase specifically, that remains an integration policy blocker pending an appropriate validated ownership flow.

The client launcher accepts only its named paths. It provides no arbitrary tML arguments, non-Steam switch or automatic ownership fallback. It checks the private patched engine hash and requires the separately prepared own dependency bootstrap at `RuntimePath/LaunchUtils/Ensure-OfficialDependenciesForLaunch.ps1`. That bootstrap must obtain official Workshop dependencies on the end user's PC; it must never download private mirrors from this repository. The full source/content bootstrap integration is not provided by these two projects alone.

Standalone dedicated servers have a separate policy. This client launcher does not expose a dedicated-server mode. Host & Play creates the normal child server after the client gate. No claim is made here that all legitimate standalone dedicated-server operation requires a personal client licence; that route needs an explicit separate distribution/policy decision.

This is a client-side gate, not unbreakable DRM: someone controlling their local machine can alter the launcher or API environment. Hash pins and fail-closed checks reduce accidental and unsupported operation; they do not create an absolute anti-tamper guarantee. A real licensed/unlicensed Steam acceptance run has not been performed by the mock tests.

Primary API references: [BIsSubscribedApp and BIsAppInstalled](https://partner.steamgames.com/doc/api/ISteamApps), [BLoggedOn](https://partner.steamgames.com/doc/api/ISteamUser#BLoggedOn), [SteamAPI initialization](https://partner.steamgames.com/doc/api/steam_api#SteamAPI_Init).
