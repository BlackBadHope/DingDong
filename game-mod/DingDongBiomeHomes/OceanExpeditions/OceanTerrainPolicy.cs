using DingDongBiomeHomes.Worldgen;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.OceanExpeditions;

internal static class OceanTerrainPolicy
{
    internal static bool[] Create()
    {
        bool[] allowed=ContinentalPlacementMaskPolicy.CreateNaturalTerrain();
        // These are ambient terrain/decorations, not authored structures.
        // The ordinary strict placement mask intentionally excludes them;
        // retaining a 48-tile shell around every stalactite blocks a whole sea.
        string[] ambient={"Marble","Granite","Stalactite","LongMoss","Pots","MinecartTrack",
            "WaterDrip","LavaDrip","HoneyDrip","SmallPiles","LargePiles","LargePiles2",
            "Seaweed","SeaOats","Plants","Plants2","JunglePlants","JunglePlants2",
            "HallowedPlants","HallowedPlants2","CorruptPlants","CrimsonPlants",
            "ImmatureHerbs","MatureHerbs","BloomingHerbs"};
        foreach(string name in ambient)if(TileID.Search.TryGetId(name,out int id))allowed[id]=true;
        // Native readback identified these untagged mod minerals. Keep all
        // foreign bricks, special biome blocks, walls and objects protected.
        foreach(string name in new[]{"SOTS/VibrantOreTile","SOTS/FakeMarble","ThoriumMod/Opal","ThoriumMod/Aquamarine"})
            if(ModContent.TryFind<ModTile>(name,out var tile))allowed[tile.Type]=true;
        return allowed;
    }
}
