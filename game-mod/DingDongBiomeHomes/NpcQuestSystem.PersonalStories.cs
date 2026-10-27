using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
namespace DingDongBiomeHomes;
internal static partial class NpcQuestSystem
{
    internal static int CountQuestSupplies(Player player, ResidentQuestDefinition d)
    {
        var step = d.StoryKey.Length > 0 ? PersonalResidentQuestCatalog.Step(d.StoryKey) : null;
        if (step == null || step.Alternatives.Length == 0) return CountInventory(player,d.ItemType);
        string tag = d.Kind == ResidentQuestKind.Expedition ? step.SourceTag : "";
        return step.Alternatives.Sum(type => CountInventory(player,type,tag));
    }
    private static void ConsumeQuestSupplies(Player player, ResidentQuestDefinition d)
    {
        var step = d.StoryKey.Length > 0 ? PersonalResidentQuestCatalog.Step(d.StoryKey) : null;
        string tag = d.Kind == ResidentQuestKind.Expedition && step != null ? step.SourceTag : "";
        int remaining = d.Goal;
        foreach (int type in step?.Alternatives.Length > 0 ? step.Alternatives : new[] { d.ItemType })
        { int count = Math.Min(remaining,CountInventory(player,type,tag)); if(count>0) ConsumeInventory(player,type,count,tag); remaining-=count; if(remaining==0) break; }
    }
    // Acquisition-tagged inventory (Expedition): only stacks whose DamageAffinity origin matches count.
    internal static int CountInventory(Player player, int itemType, string acquisitionTag)
    {
        if (string.IsNullOrEmpty(acquisitionTag)) return CountInventory(player,itemType);
        int count = 0;
        foreach (Item item in player.inventory)
            if (item.type == itemType && item.stack > 0 && item.TryGetGlobalItem(out DamageAffinityGlobalItem affinity) && string.Equals(affinity.AcquisitionTag, acquisitionTag, StringComparison.Ordinal))
                count += item.stack;
        return count;
    }
    private static void ConsumeInventory(Player player, int itemType, int count, string acquisitionTag)
    {
        if (string.IsNullOrEmpty(acquisitionTag)) { ConsumeInventory(player,itemType,count); return; }
        int remaining = count;
        for (int slot = 0; slot < player.inventory.Length && remaining > 0; slot++)
        {
            Item item = player.inventory[slot];
            if (item.type != itemType || item.stack <= 0 || !item.TryGetGlobalItem(out DamageAffinityGlobalItem affinity) || !string.Equals(affinity.AcquisitionTag, acquisitionTag, StringComparison.Ordinal)) continue;
            int removed = Math.Min(remaining, item.stack); item.stack -= removed; remaining -= removed;
            if (item.stack <= 0) item.TurnToAir();
            if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, player.whoAmI, slot, item.prefix);
        }
    }
    internal static bool ObservedTargetNearby(Player player, ResidentQuestDefinition d)
    {
        float radius = d.RadiusTiles * 16f;
        foreach (NPC npc in Main.ActiveNPCs)
            if (string.Equals(QuestContentRegistry.ContentKey(npc), d.TargetKey, StringComparison.Ordinal) && Vector2.DistanceSquared(npc.Center, player.Center) <= radius * radius)
                return true;
        return false;
    }
    internal static bool TrialStateViolated(Player player, ResidentQuestDefinition d)
    {
        var step = d.StoryKey.Length > 0 ? PersonalResidentQuestCatalog.Step(d.StoryKey) : null;
        return step != null && step.BuffType > 0 && player.HasBuff(step.BuffType);
    }
    // Gather-Env area: the resident's residence when it exists, else the resident itself, else the performer.
    internal static Point GatherCenter(Player player, string npcKey)
    {
        if (SettlementWorldState.TryGetResidence(npcKey, out SettlementResidenceReport residence)) return residence.QuestPoint(false, 0);
        foreach (NPC npc in Main.ActiveNPCs)
            if (NpcResidentRegistry.TryGet(npc, out string key, out _) && key == npcKey) return npc.Center.ToTileCoordinates();
        return player.Center.ToTileCoordinates();
    }
    internal static int CountGatheredTiles(Player player, string npcKey, ResidentQuestDefinition d)
    {
        var step = d.StoryKey.Length > 0 ? PersonalResidentQuestCatalog.Step(d.StoryKey) : null;
        if (step == null) return 0;
        Point center = GatherCenter(player, npcKey); int r = d.RadiusTiles, count = 0;
        int x0 = Math.Max(1, center.X - r), x1 = Math.Min(Main.maxTilesX - 2, center.X + r), y0 = Math.Max(1, center.Y - r), y1 = Math.Min(Main.maxTilesY - 2, center.Y + r);
        for (int x = x0; x <= x1; x++)
            for (int y = y0; y <= y1; y++)
            {
                Tile tile = Main.tile[x, y];
                if (tile.HasTile && tile.TileType == step.TileType && tile.TileFrameY == 0) count++;
            }
        return count;
    }
    internal static string ChoiceSummary(ResidentQuestDefinition d, ResidentQuestProgress progress)
    {
        var step = d.StoryKey.Length > 0 ? PersonalResidentQuestCatalog.Step(d.StoryKey) : null;
        int index = progress.Progress - 1;
        return step != null && index >= 0 && index < step.Choices.Length ? step.Choices[index].Text : "";
    }
    internal static string PersonalObjectiveText(Player p, ResidentQuestDefinition d, ResidentQuestProgress progress)
    {
        string target = PersonalResidentQuestCatalog.Resolve(NpcTownManifest.All.First(n => n.NpcKey == d.StoryKey[..d.StoryKey.LastIndexOf(':')]),d.Variant).TargetName;
        return d.Kind switch {
            ResidentQuestKind.DeliverSupplies => Language.GetTextValue("Mods.DingDongBiomeHomes.SharedQuests.Deliver",target,Math.Min(d.Goal,QuestInventoryCount(p,d,progress)),d.Goal),
            ResidentQuestKind.Expedition => Language.GetTextValue("Mods.DingDongBiomeHomes.SharedQuests.Expedition",target,Math.Min(d.Goal,QuestInventoryCount(p,d,progress)),d.Goal,Language.GetTextValue("Mods.DingDongBiomeHomes.SharedQuests.Sources."+d.TargetKey[(d.TargetKey.LastIndexOf('/')+1)..])),
            ResidentQuestKind.CreatureHunt => Language.GetTextValue("Mods.DingDongBiomeHomes.SharedQuests.Hunt",target,progress.Progress,d.Goal),
            ResidentQuestKind.Trial => Language.GetTextValue("Mods.DingDongBiomeHomes.SharedQuests.Trial",target,progress.Progress,d.Goal,Lang.GetBuffName(PersonalResidentQuestCatalog.Step(d.StoryKey)?.BuffType ?? 0)),
            ResidentQuestKind.Observe => Language.GetTextValue("Mods.DingDongBiomeHomes.SharedQuests.Observe",target,progress.Progress/60,d.Goal/60),
            ResidentQuestKind.GatherEnv => Language.GetTextValue("Mods.DingDongBiomeHomes.SharedQuests.Gather",target,progress.Progress,d.Goal),
            ResidentQuestKind.Delayed => Language.GetTextValue("Mods.DingDongBiomeHomes.SharedQuests.Delayed",Math.Min(d.Goal,progress.Progress)/PersonalResidentQuestCatalog.TicksPerDay,d.Goal/PersonalResidentQuestCatalog.TicksPerDay),
            ResidentQuestKind.DialogueChoice => progress.Progress > 0 ? Language.GetTextValue("Mods.DingDongBiomeHomes.SharedQuests.Chosen",ChoiceSummary(d,progress)) : Language.GetTextValue("Mods.DingDongBiomeHomes.SharedQuests.Choose"),
            _ => Language.GetTextValue("Mods.DingDongBiomeHomes.SharedQuests.Visit",target,progress.Progress/60,d.Goal/60) };
    }
}
