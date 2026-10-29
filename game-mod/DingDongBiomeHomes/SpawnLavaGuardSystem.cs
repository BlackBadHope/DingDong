using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

// 30.10 (owner): players died in lava right at spawn. Worldgen turns the spawn zone's lava into
// water once, but For the Worthy lava keeps flowing in while the world runs. The server re-checks
// the same zone every ten seconds and turns any lava there into water.
public sealed class SpawnLavaGuardSystem : ModSystem
{
    private const int IntervalTicks = 600, MaxSyncedCells = 4000;

    public override void PostUpdateWorld()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || Main.GameUpdateCount % IntervalTicks != 0) return;
        int spawnX = Main.spawnTileX, spawnY = Main.spawnTileY;
        if (spawnX <= 0 || spawnY <= 0) return;
        int left = Math.Max(1, spawnX - SpawnSanctuaryWorldgenSystem.SpawnLavaHalfWidth), right = Math.Min(Main.maxTilesX - 2, spawnX + SpawnSanctuaryWorldgenSystem.SpawnLavaHalfWidth);
        int top = Math.Max(1, spawnY - SpawnSanctuaryWorldgenSystem.SpawnLavaAbove), bottom = Math.Min(Main.maxTilesY - 2, spawnY + SpawnSanctuaryWorldgenSystem.SpawnLavaBelow);
        int converted = 0;
        for (int x = left; x <= right; x++)
        for (int y = top; y <= bottom; y++)
        {
            Tile tile = Main.tile[x, y];
            if (tile.LiquidAmount == 0 || tile.LiquidType != LiquidID.Lava) continue;
            tile.LiquidType = LiquidID.Water;
            Liquid.AddWater(x, y);
            if (Main.netMode == NetmodeID.Server && converted < MaxSyncedCells) NetMessage.sendWater(x, y);
            converted++;
        }
        if (converted > 0) Mod.Logger.Info($"DINGDONG_SPAWN_LAVA_GUARD|converted={converted}|zone={left},{top},{right},{bottom}");
    }
}
