using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

internal static class ResidentResidenceProtection
{
    internal static bool IsProtected(int x, int y)
    {
        // World generation and loading must retain their native write path.
        if (Main.gameMenu || WorldGen.gen || !ModContent.GetInstance<DingDongServerConfig>().EnableNpcQuestsAndEscort)
            return false;
        foreach (SettlementResidenceReport home in SettlementWorldState.Residences)
            if (home.Area.Contains(x, y) && !NpcResidenceWorldSystem.IsOriginReleased(home.NpcKey)) return true;
        return false;
    }

    // Only the built house is guarded. Plants, terrain, ore and graves stay free:
    // vanilla removes some of them in loops (CheckCatTail) and a refused kill hangs the server.
    internal static bool IsProtected(int x, int y, int type) => !IsNatural(type) && IsProtected(x, y);

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
    private static bool ReplaceTile(On_WorldGen.orig_ReplaceTile next, int x, int y, ushort type, int style) =>
        !ResidentResidenceProtection.IsProtected(x, y) && next(x, y, type, style);
    private static bool ReplaceWall(On_WorldGen.orig_ReplaceWall next, int x, int y, ushort type) =>
        !ResidentResidenceProtection.IsProtected(x, y) && next(x, y, type);
    private static bool SlopeTile(On_WorldGen.orig_SlopeTile next, int x, int y, int slope, bool effects) =>
        !ResidentResidenceProtection.IsProtected(x, y) && next(x, y, slope, effects);
    private static bool PoundTile(On_WorldGen.orig_PoundTile next, int x, int y) =>
        !ResidentResidenceProtection.IsProtected(x, y) && next(x, y);
}

public sealed class ResidentResidenceGlobalTile : GlobalTile
{
    public override bool CanKillTile(int i, int j, int type, ref bool blockDamaged) => !ResidentResidenceProtection.IsProtected(i, j, type);
    public override bool CanExplode(int i, int j, int type) => !ResidentResidenceProtection.IsProtected(i, j, type);
    public override bool CanReplace(int i, int j, int type, int tileTypeBeingPlaced) => !ResidentResidenceProtection.IsProtected(i, j);
    public override bool Slope(int i, int j, int type) => !ResidentResidenceProtection.IsProtected(i, j);
    public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
    {
        if (!ResidentResidenceProtection.IsProtected(i, j, type)) return;
        fail = true;
        noItem = true;
    }
}

public sealed class ResidentResidenceGlobalWall : GlobalWall
{
    public override bool CanExplode(int i, int j, int type) => !ResidentResidenceProtection.IsProtected(i, j);
    public override void KillWall(int i, int j, int type, ref bool fail)
    {
        if (ResidentResidenceProtection.IsProtected(i, j)) fail = true;
    }
}
