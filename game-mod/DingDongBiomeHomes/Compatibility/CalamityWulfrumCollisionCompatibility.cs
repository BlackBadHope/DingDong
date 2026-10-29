using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Compatibility;

// 30.10 (owner): the Wulfrum robots (drone, hovercraft and the rest of the series) fly through
// blocks, spawn off-screen and cannot be reached early in the game. Their AI may turn tile
// collision off every tick, so it is turned back on after AI, before the movement step.
public sealed class CalamityWulfrumCollisionCompatibility : GlobalNPC
{
    public override bool AppliesToEntity(NPC entity, bool lateInstantiation) =>
        lateInstantiation && entity.ModNPC is { } modNpc && modNpc.Mod.Name == "CalamityMod" && modNpc.Name.Contains("Wulfrum");

    public override void PostAI(NPC npc) => npc.noTileCollide = false;
}
