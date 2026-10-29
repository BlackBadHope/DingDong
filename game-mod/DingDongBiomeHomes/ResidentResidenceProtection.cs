using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

// Guards exactly the blocks and walls the authored house placed, until its resident is released.
// Terrain, plants, grown decor and player-placed tiles inside the scene stay breakable.
internal static class ResidentResidenceProtection
{
    private static readonly Dictionary<(int, int, int), DingDongAuthoredStructureLoader.ProtectedFootprint> footprints = new();
    private static readonly HashSet<(int, int, int)> unresolved = new();

    internal static void Reset()
    {
        footprints.Clear();
        unresolved.Clear();
    }

    internal static bool IsTileProtected(int x, int y, int type) => !IsNatural(type) && Covers(x, y, wall: false);
    internal static bool IsTileProtected(int x, int y) => IsTileProtected(x, y, Main.tile[x, y].TileType);
    internal static bool IsWallProtected(int x, int y) => Covers(x, y, wall: true);

    private static bool Covers(int x, int y, bool wall)
    {
        // World generation and loading must retain their native write path.
        if (Main.gameMenu || WorldGen.gen || !ModContent.GetInstance<DingDongServerConfig>().EnableNpcQuestsAndEscort)
            return false;
        foreach (SettlementResidenceReport home in SettlementWorldState.Residences)
        {
            if (!home.Area.Contains(x, y) || NpcResidenceWorldSystem.IsOriginReleased(home.NpcKey)) continue;
            var footprint = Footprint(home);
            if (footprint == null) return false;
            int index = (x - home.Area.X) * footprint.Height + (y - home.Area.Y);
            return wall ? footprint.Walls[index] : footprint.Tiles[index];
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

    // Plants, terrain, ore and graves are never guarded, even inside the footprint:
    // vanilla removes some of them itself in loops (CheckCatTail) and a refused kill hangs the server.
    private static bool IsNatural(int type) =>
        Main.tileCut[type] || type is TileID.Tombstones or TileID.Cattail or TileID.LilyPad or TileID.Dirt or TileID.Mud or TileID.ClayBlock
            or TileID.SnowBlock or TileID.Silt or TileID.Slush or TileID.Ash or TileID.Trees or TileID.PalmTree
        || TileID.Sets.Ore[type] || TileID.Sets.Grass[type] || TileID.Sets.Conversion.Stone[type]
        || TileID.Sets.Conversion.Sand[type] || TileID.Sets.Conversion.Moss[type];
}

// These native replacement paths bypass the local-client CanReplace hook.
// Guard the actual mutation as well, including server-side packet handling.
public sealed class ResidentResidenceMutationSystem : ModSystem
{
    public override void Load()
    {
        On_WorldGen.ReplaceTile += ReplaceTile;
        On_WorldGen.ReplaceWall += ReplaceWall;
        On_WorldGen.SlopeTile += SlopeTile;
        On_WorldGen.PoundTile += PoundTile;
    }
    public override void Unload()
    {
        On_WorldGen.ReplaceTile -= ReplaceTile;
        On_WorldGen.ReplaceWall -= ReplaceWall;
        On_WorldGen.SlopeTile -= SlopeTile;
        On_WorldGen.PoundTile -= PoundTile;
    }
    public override void OnWorldLoad() => ResidentResidenceProtection.Reset();
    public override void OnWorldUnload() => ResidentResidenceProtection.Reset();
    private static bool ReplaceTile(On_WorldGen.orig_ReplaceTile next, int x, int y, ushort type, int style) =>
        !ResidentResidenceProtection.IsTileProtected(x, y) && next(x, y, type, style);
    private static bool ReplaceWall(On_WorldGen.orig_ReplaceWall next, int x, int y, ushort type) =>
        !ResidentResidenceProtection.IsWallProtected(x, y) && next(x, y, type);
    private static bool SlopeTile(On_WorldGen.orig_SlopeTile next, int x, int y, int slope, bool effects) =>
        !ResidentResidenceProtection.IsTileProtected(x, y) && next(x, y, slope, effects);
    private static bool PoundTile(On_WorldGen.orig_PoundTile next, int x, int y) =>
        !ResidentResidenceProtection.IsTileProtected(x, y) && next(x, y);
}

public sealed class ResidentResidenceGlobalTile : GlobalTile
{
    public override bool CanKillTile(int i, int j, int type, ref bool blockDamaged) => !ResidentResidenceProtection.IsTileProtected(i, j, type);
    public override bool CanExplode(int i, int j, int type) => !ResidentResidenceProtection.IsTileProtected(i, j, type);
    public override bool CanReplace(int i, int j, int type, int tileTypeBeingPlaced) => !ResidentResidenceProtection.IsTileProtected(i, j, type);
    public override bool Slope(int i, int j, int type) => !ResidentResidenceProtection.IsTileProtected(i, j, type);
    public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
    {
        // Multi-tile objects drop their item in vanilla's frame check before killing the parts;
        // refusing those kills leaves the object standing and drops again on every check (dupe).
        // Players are already stopped by CanKillTile, so only plain blocks are refused here.
        if (Main.tileFrameImportant[type] || !ResidentResidenceProtection.IsTileProtected(i, j, type)) return;
        fail = true;
        noItem = true;
    }
}

public sealed class ResidentResidenceGlobalWall : GlobalWall
{
    public override bool CanExplode(int i, int j, int type) => !ResidentResidenceProtection.IsWallProtected(i, j);
    public override void KillWall(int i, int j, int type, ref bool fail)
    {
        if (ResidentResidenceProtection.IsWallProtected(i, j)) fail = true;
    }
}
