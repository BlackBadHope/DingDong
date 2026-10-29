using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace DingDongBiomeHomes;

// S23 (owner, 30.10): residences are no longer locked. Every block can be broken; breaking a
// block the authored house itself placed costs reputation with its resident (ResidentReputationSystem).
// The footprint is read from the house's blueprint, so terrain, plants and player-placed tiles cost nothing.
internal static class ResidentResidenceProtection
{
    private static readonly Dictionary<(int, int, int), DingDongAuthoredStructureLoader.ProtectedFootprint> footprints = new();
    private static readonly HashSet<(int, int, int)> unresolved = new();

    internal static void Reset()
    {
        footprints.Clear();
        unresolved.Clear();
    }

    /// <summary>The resident whose authored house placed this cell, while that resident still lives there.</summary>
    internal static bool TryOwner(int x, int y, bool wall, out string npcKey)
    {
        npcKey = null;
        if (Main.gameMenu || WorldGen.gen || !ModContent.GetInstance<DingDongServerConfig>().EnableNpcQuestsAndEscort)
            return false;
        foreach (SettlementResidenceReport home in SettlementWorldState.Residences)
        {
            if (!home.Area.Contains(x, y) || NpcResidenceWorldSystem.IsOriginReleased(home.NpcKey)) continue;
            var footprint = Footprint(home);
            if (footprint == null) return false;
            int index = (x - home.Area.X) * footprint.Height + (y - home.Area.Y);
            if (!(wall ? footprint.Walls[index] : footprint.Tiles[index])) return false;
            npcKey = home.NpcKey;
            return true;
        }
        return false;
    }

    private static DingDongAuthoredStructureLoader.ProtectedFootprint Footprint(SettlementResidenceReport home)
    {
        var key = (home.ReviewId, home.Area.X, home.Area.Y);
        if (footprints.TryGetValue(key, out var cached)) return cached;
        if (unresolved.Contains(key)) return null;
        DingDongAuthoredStructureLoader.ProtectedFootprint built = null;
        try
        {
            using (DingDongAuthoredStructureLoader.UseTransientCaches())
                built = DingDongAuthoredStructureLoader.BuildProtectedFootprint(ModContent.GetInstance<DingDongBiomeHomes>(), home);
        }
        catch (System.Exception error)
        {
            ModContent.GetInstance<DingDongBiomeHomes>().Logger.Warn($"DINGDONG_RESIDENCE_FOOTPRINT|npc={home.NpcKey}|status=UNRESOLVED|error={error.GetType().Name}");
        }
        if (built == null) { unresolved.Add(key); return null; }
        footprints[key] = built;
        return built;
    }

    // Plants, terrain, ore and graves never cost reputation: they are not the house itself.
    internal static bool IsNatural(int type) =>
        Main.tileCut[type] || type is TileID.Tombstones or TileID.Cattail or TileID.LilyPad or TileID.Dirt or TileID.Mud or TileID.ClayBlock
            or TileID.SnowBlock or TileID.Silt or TileID.Slush or TileID.Ash or TileID.Trees or TileID.PalmTree or TileID.LandMine
        || TileID.Sets.Ore[type] || TileID.Sets.Grass[type] || TileID.Sets.Conversion.Stone[type]
        || TileID.Sets.Conversion.Sand[type] || TileID.Sets.Conversion.Moss[type];
}

public sealed class ResidentResidenceMutationSystem : ModSystem
{
    public override void OnWorldLoad() => ResidentResidenceProtection.Reset();
    public override void OnWorldUnload() => ResidentResidenceProtection.Reset();
}

public sealed class ResidentResidenceGlobalTile : GlobalTile
{
    public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
    {
        if (fail || effectOnly || Main.netMode == NetmodeID.MultiplayerClient) return;
        // A multi-tile object is charged once, at its top-left cell.
        if (Main.tileFrameImportant[type] && TileObjectData.GetTileData(type, 0) != null && !TileObjectData.IsTopLeft(i, j)) return;
        if (!ResidentResidenceProtection.TryOwner(i, j, wall: false, out string npcKey)) return;
        int style = System.Math.Max(0, TileObjectData.GetTileStyle(Main.tile[i, j]));
        ResidentReputationSystem.OnAuthoredCellBroken(npcKey, i, j, type, style, wall: false);
    }
}

public sealed class ResidentResidenceGlobalWall : GlobalWall
{
    public override void KillWall(int i, int j, int type, ref bool fail)
    {
        if (fail || Main.netMode == NetmodeID.MultiplayerClient) return;
        if (ResidentResidenceProtection.TryOwner(i, j, wall: true, out string npcKey))
            ResidentReputationSystem.OnAuthoredCellBroken(npcKey, i, j, type, 0, wall: true);
    }
}
