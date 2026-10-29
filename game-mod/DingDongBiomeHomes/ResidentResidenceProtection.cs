using Terraria;
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
    public override bool CanKillTile(int i, int j, int type, ref bool blockDamaged) => !ResidentResidenceProtection.IsProtected(i, j);
    public override bool CanExplode(int i, int j, int type) => !ResidentResidenceProtection.IsProtected(i, j);
    public override bool CanReplace(int i, int j, int type, int tileTypeBeingPlaced) => !ResidentResidenceProtection.IsProtected(i, j);
    public override bool Slope(int i, int j, int type) => !ResidentResidenceProtection.IsProtected(i, j);
    public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
    {
        if (!ResidentResidenceProtection.IsProtected(i, j)) return;
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
