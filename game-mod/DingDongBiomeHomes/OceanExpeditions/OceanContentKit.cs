using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes.OceanExpeditions;

// Small worldgen helpers shared by the open-ocean seabed content and the
// Abyss waystations. Every helper writes only the cells it is given; callers
// validate ownership (empty water inside Ding Dong's own carve) first.
internal static class OceanContentKit
{
    internal static void Solid(int x, int y, ushort type)
    {
        Tile t = Main.tile[x, y];
        t.ClearEverything();
        t.HasTile = true;
        t.TileType = type;
    }

    // Abyss variant: the carve there keeps Calamity's background walls (it only
    // clears tiles), so content written into it must keep the wall and its paint.
    internal static void SolidKeepWall(int x, int y, ushort type)
    {
        Tile t = Main.tile[x, y];
        t.ClearTile();
        t.HasTile = true;
        t.TileType = type;
        t.LiquidAmount = 0;
        t.Slope = SlopeType.Solid;
        t.IsHalfBlock = false;
    }

    // Underwater content must never leave a dry pocket: the sea settle/refill
    // receipt counts such cells. Any non-solid cell of the area becomes full water.
    internal static void Flood(Rectangle area)
    {
        for (int x = area.Left; x < area.Right; x++)
        for (int y = area.Top; y < area.Bottom; y++)
        {
            Tile t = Main.tile[x, y];
            if (t.HasTile && Main.tileSolid[t.TileType] && !Main.tileSolidTop[t.TileType]) continue;
            t.LiquidType = LiquidID.Water;
            t.LiquidAmount = 255;
        }
    }

    internal static bool NoChestNear(int x, int y, int radius)
    {
        foreach (Chest chest in Main.chest)
            if (chest != null && Math.Abs(chest.x - x) < radius && Math.Abs(chest.y - y) < radius) return false;
        return true;
    }

    // A two-tile pedestal plus a chest standing on it; returns the chest index or -1.
    internal static int LootChest(int x, int floorRow, ushort pedestal, int style, (int Type, int Stack)[] loot, bool keepWall = false)
    {
        for (int dx = 0; dx < 2; dx++)
        {
            if (keepWall) SolidKeepWall(x + dx, floorRow, pedestal);
            else Solid(x + dx, floorRow, pedestal);
            for (int dy = -2; dy < 0; dy++) Main.tile[x + dx, floorRow + dy].ClearTile();
        }
        int chest = WorldGen.PlaceChest(x, floorRow - 1, TileID.Containers, false, style);
        if (chest < 0 || Main.chest[chest] == null) return -1;
        Item[] items = Main.chest[chest].item;
        for (int i = 0; i < loot.Length && i < items.Length; i++)
        {
            items[i].SetDefaults(loot[i].Type);
            items[i].stack = Math.Clamp(loot[i].Stack, 1, Math.Max(1, items[i].maxStack));
        }
        return chest;
    }

    // Grows a seaweed chain from the solid sand at floorRow upwards.
    internal static int Seaweed(int x, int floorRow, int height)
    {
        int grown = 0;
        for (int y = floorRow - 1; y > floorRow - 1 - height; y--)
        {
            Tile t = Main.tile[x, y];
            if (t.HasTile) break;
            WorldGen.PlaceUnderwaterPlant(TileID.Seaweed, x, y);
            if (!t.HasTile || t.TileType != TileID.Seaweed) break;
            grown++;
        }
        return grown;
    }
}
