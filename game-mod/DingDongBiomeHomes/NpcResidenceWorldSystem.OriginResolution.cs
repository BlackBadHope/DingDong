using System.Diagnostics;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes;

public sealed partial class NpcResidenceWorldSystem
{
    private static readonly OriginHousingScanQueue<ResidentEscortState> OriginScans = new();

    private static void ProcessOriginScans()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || Main.gameMenu || OriginScans.Count == 0) return;
        long started = Stopwatch.GetTimestamp();
        int resolved = OriginScans.Pump(Main.GameUpdateCount,
            () => Stopwatch.GetElapsedTime(started).TotalMilliseconds < 2d,
            IsOriginHousingCandidate, ValidateOriginHousing,
            (state, home) => { state.OriginHome = new Point(home.X, home.Y); Touch(state); });
        if (resolved > 0) BroadcastSnapshot();
    }

    private static bool IsOriginHousingCandidate(int x, int y)
    {
        if (!WorldGen.InWorld(x, y, 12) || WorldGen.SolidTile(x, y)) return false;
        ushort wall = Main.tile[x, y].WallType;
        return wall > 0 && wall < Main.wallHouse.Length && Main.wallHouse[wall];
    }

    private static bool ValidateOriginHousing(int x, int y, int npcType, out OriginScanPoint home)
    {
        if (AuthoredSettlementBuilder.TryValidateOriginPoint(new Point(x, y), npcType, out Point point))
        { home = new OriginScanPoint(point.X, point.Y); return true; }
        home = default;
        return false;
    }
}
