using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Compatibility;

// Owner, 27.10: auto-trash must not mix up prefixed items. AutoTrash matches its list by item
// type only, so a plain sword on the list also trashed a Legendary one on pickup. A pickup whose
// type is listed is trashed only when the list holds that exact prefix; otherwise it is kept.
public sealed class AutoTrashPrefixGuard : ModSystem
{
    private delegate bool OrigOnPickup(GlobalItem self, Item item, Player player);
    private delegate bool HookOnPickup(OrigOnPickup orig, GlobalItem self, Item item, Player player);

    public override void Load()
    {
        if (!ModLoader.TryGetMod("AutoTrash", out Mod autoTrash)) return;
        MethodInfo onPickup = autoTrash.Code?.GetType("AutoTrash.AutoTrashGlobalItem")?
            .GetMethod("OnPickup", BindingFlags.Public | BindingFlags.Instance, binder: null, new[] { typeof(Item), typeof(Player) }, modifiers: null);
        if (onPickup == null)
        {
            Mod.Logger.Warn($"DINGDONG_AUTOTRASH_PREFIX_GUARD|verdict=SKIPPED|version={autoTrash.Version}|reason=no-OnPickup");
            return;
        }
        MonoModHooks.Add(onPickup, (HookOnPickup)Guard);
        Mod.Logger.Info($"DINGDONG_AUTOTRASH_PREFIX_GUARD|verdict=ARMED|version={autoTrash.Version}");
    }

    private static bool Guard(OrigOnPickup orig, GlobalItem self, Item item, Player player)
    {
        if (AutoTrashSlot.Installed)
        {
            var rules = AutoTrashSlot.Filters(player);
            if (rules.Any(rule => rule.type == item.type) && !rules.Any(rule => rule.type == item.type && rule.prefix == item.prefix))
                return true;
        }
        return orig(self, item, player);
    }
}
