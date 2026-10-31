using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.OceanExpeditions;

// Owner, 31.10 ("combine everything"): the open sea is dressed with the ocean decor of the mods in
// the pack instead of bare sand. Calamity's Sunken Sea ambient tiles (corals, anemones, kelp bushes,
// driftwood, a sunken ship) and, when installed, Spirit Reforged's ocean set (corals, decor, tall
// kelp, seagrass, mussels, hydrothermal vents on gravel and magmastone). Only decoration is used:
// Calamity's biome counters (Eutrophic sand, Navystone, coloured coral blocks, pearl piles, scarlet
// seagrass...) are never placed, so the sea stays the Ocean, not the Sunken Sea. Every type is looked
// up by name; a missing mod or a renamed tile only removes that decoration.
// Ideas from Spirit Mod (MIT, GabeHasWon/SpiritMod OceanGeneration): dense kelp and coral on the
// first slope, rarer decor deeper, gravel patches with vents in the deep. No code or art is copied.
internal static class OceanModDecor
{
    private static bool resolved;
    private static ushort[] corals = Array.Empty<ushort>(), smallCorals = Array.Empty<ushort>(), piles = Array.Empty<ushort>(),
        wrecks = Array.Empty<ushort>(), spiritCorals = Array.Empty<ushort>(), spiritDecor = Array.Empty<ushort>();
    private static ushort anemone, kelpBush, sunkenShip, spiritKelp, spiritSeagrass, spiritMussel, spiritVent, gravel, magmastone,
        gravelPile, gravelStalagmite, thermalVent;

    internal static bool Spirit => spiritKelp != 0;

    private static ushort Find(Mod mod, string name) => mod != null && mod.TryFind(name, out ModTile tile) ? tile.Type : (ushort)0;
    private static ushort[] All(Mod mod, params string[] names)
    {
        var list = new List<ushort>();
        foreach (string name in names) { ushort t = Find(mod, name); if (t != 0) list.Add(t); }
        return list.ToArray();
    }

    internal static void Resolve()
    {
        if (resolved) return;
        resolved = true;
        ModLoader.TryGetMod("CalamityMod", out Mod cal);
        ModLoader.TryGetMod("SpiritReforged", out Mod spirit);
        corals = All(cal, "FanCoral", "BrainCoral", "TableCoral", "TubeCoral", "StalkCoral", "BlueCoralTree", "BranchCoral",
            "BrownCoral", "DigitateCoral", "TallDigitateCoral", "FryCoral", "MediumCoral");
        smallCorals = All(cal, "SmallCorals", "SmallBrainCoral", "SmallTubeCoral", "SmallWideCoral");
        piles = All(cal, "CoralPileLarge", "CoralPileGiant");
        wrecks = All(cal, "DriftwoodWreckage", "LargeDriftwoodWreckage", "DriftwoodMast", "DriftwoodScraps");
        anemone = Find(cal, "SeaAnemone"); kelpBush = Find(cal, "KelpBush"); sunkenShip = Find(cal, "SunkenShip");
        thermalVent = Find(cal, "ThermalVent");
        spiritCorals = All(spirit, "Coral3x3", "Coral2x2", "Coral1x2");
        spiritDecor = All(spirit, "OceanDecor2x3", "OceanDecor2x2", "OceanDecor1x2");
        spiritKelp = Find(spirit, "OceanKelp"); spiritSeagrass = Find(spirit, "Seagrass"); spiritMussel = Find(spirit, "Mussel");
        spiritVent = Find(spirit, "HydrothermalVent"); gravel = Find(spirit, "Gravel"); magmastone = Find(spirit, "Magmastone");
        gravelPile = Find(spirit, "GravelPile"); gravelStalagmite = Find(spirit, "GravelStalagmite");
    }

    internal static string Inventory() =>
        $"calamityCorals:{corals.Length},calamitySmall:{smallCorals.Length},piles:{piles.Length},wrecks:{wrecks.Length},ship:{(sunkenShip != 0 ? 1 : 0)},spirit:{(Spirit ? 1 : 0)}";

    // Places a (multi)tile whose bottom row rests on the solid row below (x, y); true when it stands.
    internal static bool Place(int x, int y, ushort type, Random random)
    {
        if (type == 0 || Main.tile[x, y].HasTile) return false;
        WorldGen.PlaceTile(x, y, type, mute: true, style: 0);
        Tile t = Main.tile[x, y];
        if (!t.HasTile || t.TileType != type) return false;
        return true;
    }

    private static ushort Pick(ushort[] set, Random random) => set.Length == 0 ? (ushort)0 : set[random.Next(set.Length)];

    // Tall kelp: Spirit's kelp column when installed, otherwise vanilla seaweed.
    internal static int Kelp(int x, int ground, int height, Random random)
    {
        if (spiritKelp != 0)
        {
            int grown = 0;
            for (int h = 0; h < height; h++)
            {
                int y = ground - 1 - h;
                if (Main.tile[x, y].HasTile) break;
                WorldGen.PlaceTile(x, y, spiritKelp, mute: true);
                if (!Main.tile[x, y].HasTile || Main.tile[x, y].TileType != spiritKelp) break;
                grown++;
            }
            if (grown > 0) return grown;
        }
        if (kelpBush != 0 && random.Next(4) == 0 && Place(x, ground - 1, kelpBush, random)) return 1;
        return OceanContentKit.Seaweed(x, ground, height);
    }

    // Floor dressing by zone: the reef flat and first slope are dense, the open floor sparse.
    // Returns 1 when something was placed on (x, ground-1).
    internal static int Dress(int x, int ground, bool reef, double depthShare, Random random)
    {
        int roll = random.Next(1000);
        if (reef)
        {
            if (roll < 260) return Kelp(x, ground, 5 + random.Next(reef ? 26 : 10), random) > 0 ? 1 : 0;
            if (roll < 330) return Place(x, ground - 1, Pick(spiritCorals.Length > 0 && random.Next(2) == 0 ? spiritCorals : corals, random), random) ? 1 : 0;
            if (roll < 370) return Place(x, ground - 1, Pick(smallCorals, random), random) ? 1 : 0;
            if (roll < 395) return Place(x, ground - 1, Pick(spiritDecor, random), random) ? 1 : 0;
            if (roll < 410) return Place(x, ground - 1, spiritSeagrass, random) || Place(x, ground - 1, anemone, random) ? 1 : 0;
            if (roll < 418) return Place(x, ground - 1, spiritMussel, random) ? 1 : 0;
            return 0;
        }
        if (roll < 22) return Place(x, ground - 1, Pick(corals, random), random) ? 1 : 0;
        if (roll < 40) return Place(x, ground - 1, Pick(smallCorals, random), random) ? 1 : 0;
        if (roll < 52) return Place(x, ground - 1, anemone, random) ? 1 : 0;
        if (roll < 62) return Place(x, ground - 1, Pick(piles, random), random) ? 1 : 0;
        if (roll < 70) return Place(x, ground - 1, Pick(spiritDecor, random), random) ? 1 : 0;
        if (roll < 76 && depthShare < .6) return Place(x, ground - 1, Pick(wrecks, random), random) ? 1 : 0;
        if (roll < 110) return Kelp(x, ground, 3 + random.Next(9), random) > 0 ? 1 : 0;
        return 0;
    }

    internal static bool SunkenShip(int x, int ground, Random random) => Place(x, ground - 1, sunkenShip, random);

    // A vent field: the sand of a small blotch becomes gravel ringed by magmastone (Spirit) or
    // obsidian, with vents and gravel piles on top. Owned floor cells only (the caller checks).
    internal static int VentField(int cx, int ground, Func<int, int> groundAt, Func<int, int, bool> owned, Random random)
    {
        int radius = 5 + random.Next(9), placed = 0;
        ushort core = gravel != 0 ? gravel : TileID.Obsidian, rim = magmastone != 0 ? magmastone : TileID.Stone;
        for (int dx = -radius - 2; dx <= radius + 2; dx++)
        {
            int x = cx + dx, g = groundAt(x);
            for (int dy = 0; dy < 4; dy++)
            {
                int y = g + dy;
                if (!owned(x, y) || !Main.tile[x, y].HasTile) continue;
                if (Math.Abs(dx) <= radius - random.Next(2)) Main.tile[x, y].TileType = core;
                else if (Math.Abs(dx) <= radius + 2 && random.Next(2) == 0) Main.tile[x, y].TileType = rim;
            }
        }
        ushort vent = spiritVent != 0 ? spiritVent : thermalVent;
        for (int k = 0; k < 2 + radius / 3; k++)
        {
            int x = cx + random.Next(-radius, radius + 1), g = groundAt(x);
            if (!owned(x, g - 1)) continue;
            if (Place(x, g - 1, vent, random)) placed++;
            else if (Place(x, g - 1, random.Next(2) == 0 ? gravelPile : gravelStalagmite, random)) placed++;
        }
        return placed;
    }
}
