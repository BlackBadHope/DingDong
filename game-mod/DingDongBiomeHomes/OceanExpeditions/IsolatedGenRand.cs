using System;
using System.Reflection;
using Terraria;
using Terraria.Utilities;

namespace DingDongBiomeHomes.OceanExpeditions;

// Decorative worldgen added late in the pipeline (seabed points of interest,
// Abyss waystations) must not shift the shared world random stream: every
// later pass, including the residence planner, keeps the exact sequence it had
// before this content existed. Vanilla helpers such as PlaceTile/TileFrame read
// WorldGen.genRand, so they run against a private seeded generator here.
internal static class IsolatedGenRand
{
    private static readonly FieldInfo Field =
        typeof(WorldGen).GetField("_genRand", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

    // Returns true when the shared stream was isolated; false means the field
    // was unavailable and the action ran against the shared generator.
    internal static bool Run(int seed, Action action)
    {
        object saved = Field is { IsInitOnly: false } ? Field.GetValue(null) : null;
        if (saved == null)
        {
            action();
            return false;
        }
        try { Field.SetValue(null, new UnifiedRandom(seed)); }
        catch (Exception error) when (error is FieldAccessException or ArgumentException)
        {
            action();
            return false;
        }
        try { action(); }
        finally { Field.SetValue(null, saved); }
        return true;
    }
}
