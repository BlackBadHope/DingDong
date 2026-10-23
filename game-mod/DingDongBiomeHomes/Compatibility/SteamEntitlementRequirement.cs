using System;
using Steamworks;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Compatibility;

/// <summary>Client access policy for the public edition; no Steam/game binary is distributed.</summary>
public sealed class SteamEntitlementRequirement : ModSystem
{
    public override void Load()
    {
        // Dedicated servers follow tModLoader's server licensing policy. They do
        // not impersonate an interactive Steam user; Host & Play checks its client.
        if (Main.dedServ)
            return;

        const string explanation = "DingDong Public requires the official Steam tModLoader, " +
            "a running online Steam session and a verified Terraria entitlement (105600). " +
            "Start the official tModLoader (1281930) through Steam. " +
            "DingDong does not contain Terraria or a non-Steam fallback.";
        try
        {
            // tModLoader owns SteamAPI initialization and shutdown. Never change
            // its session or call Init/Shutdown from a mod's load hook.
            if (!SteamAPI.IsSteamRunning() || !SteamUser.BLoggedOn() ||
                SteamUtils.GetAppID().m_AppId != 1281930 ||
                !SteamApps.BIsSubscribedApp(new AppId_t(105600)) ||
                !SteamApps.BIsAppInstalled(new AppId_t(105600)) ||
                !SteamApps.BIsAppInstalled(new AppId_t(1281930)))
                throw new InvalidOperationException(explanation);
        }
        catch (Exception error)
        {
            throw new InvalidOperationException(explanation, error);
        }
    }
}
