using Terraria.ModLoader;
using Terraria.UI;

namespace DingDongBiomeHomes.Compatibility;

// 30.10 (owner): Fargo's Souls opens two large warnings on every world entry: the Calamity DLC
// is "missing" and Fargo's Music Mod is "not found". The Ding Dong pack ships without both on
// purpose, so those two in-game notifications are dropped; every other notification passes.
public sealed class FargoNotificationFilter : ModSystem
{
    public override void Load() => On_InGameNotificationsTracker.AddNotification += Filter;
    public override void Unload() => On_InGameNotificationsTracker.AddNotification -= Filter;

    private static void Filter(On_InGameNotificationsTracker.orig_AddNotification orig, IInGameNotification notification)
    {
        System.Type type = notification?.GetType();
        if (type != null && type.Namespace?.StartsWith("FargowiltasSouls") == true && type.Name is "NoDLCNotification" or "NoMusicNotification")
            return;
        orig(notification);
    }
}
