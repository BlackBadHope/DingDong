using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Fishing.Encounters;

public readonly record struct FishingEnemySpec(string NameEn, string NameRu, string EventId,
	FishingHabitat Habitat, FishingEnemyBehavior Behavior, int TextureNpc, int RequiredProgression,
	int Life, int Damage, int Defense, int Width, int Height, int NativeDrop,
	string MaterialId, Color Tint, int ContactDebuff = 0);

public static class FishingEnemyCatalog
{
	// Index matches the corresponding immutable event ID, not the NPC's assigned runtime type.
	public static IReadOnlyList<FishingEnemySpec> Definitions { get; } = Array.AsReadOnly(new[]
	{
		new FishingEnemySpec("Shoal Snapper", "Прибрежный хвататель", "shoal_run", FishingHabitat.CoastalShoals, FishingEnemyBehavior.CrabLunge, NPCID.Crab, 0, 65, 12, 6, 34, 24, ItemID.Coral, "tide_pearl", new Color(229, 168, 96)),
		new FishingEnemySpec("Tide Bell", "Приливный колокол", "tidal_wash", FishingHabitat.CoastalShoals, FishingEnemyBehavior.TidePulse, NPCID.PinkJellyfish, 1, 110, 18, 3, 28, 32, ItemID.Gel, "coral_fiber", new Color(121, 234, 214), BuffID.Slow),
		new FishingEnemySpec("Reef Charger", "Рифовый таран", "feeding_frenzy", FishingHabitat.OpenOcean, FishingEnemyBehavior.ReefCharge, NPCID.Shark, 1, 180, 24, 8, 64, 28, ItemID.SharkFin, "ocean_glass", new Color(132, 192, 230)),
		new FishingEnemySpec("Storm Squid", "Штормовой кальмар", "storm_drift", FishingHabitat.OpenOcean, FishingEnemyBehavior.SquidRetreat, NPCID.Squid, 3, 340, 34, 12, 36, 38, ItemID.BlackInk, "storm_scale", new Color(156, 176, 246), BuffID.Blackout),
		new FishingEnemySpec("Lantern Maw", "Фонарная пасть", "lantern_bloom", FishingHabitat.DeepOcean, FishingEnemyBehavior.LanternAmbush, NPCID.AnglerFish, 2, 250, 28, 10, 42, 30, ItemID.Glowstick, "lantern_oil", new Color(170, 216, 132)),
		new FishingEnemySpec("Pressure Eel", "Барический угорь", "pressure_surge", FishingHabitat.DeepOcean, FishingEnemyBehavior.EelSlalom, NPCID.GiantWormHead, 4, 540, 44, 18, 38, 26, ItemID.Bone, "pressure_crystal", new Color(151, 201, 219)),
		new FishingEnemySpec("Blackwater Stalker", "Преследователь черноводья", "black_current", FishingHabitat.Abyss, FishingEnemyBehavior.ShadowStalk, NPCID.BlueJellyfish, 3, 680, 48, 22, 32, 38, ItemID.SoulofNight, "abyssal_steel", new Color(120, 106, 171), BuffID.Darkness),
		new FishingEnemySpec("Wake Carapace", "Панцирь глубинного следа", "leviathan_wake", FishingHabitat.Abyss, FishingEnemyBehavior.ShellGuard, NPCID.GiantShelly, 6, 1600, 76, 34, 46, 36, ItemID.BlackPearl, "void_pearl", new Color(119, 172, 174)),
		new FishingEnemySpec("Crystal Urchin", "Кристальный иглобрюх", "crystal_hatch", FishingHabitat.CaveLake, FishingEnemyBehavior.CrystalVolley, NPCID.SpikedIceSlime, 1, 140, 20, 10, 30, 26, ItemID.Amethyst, "cave_salt", new Color(207, 168, 244)),
		new FishingEnemySpec("Blind Lake Hunter", "Слепой озёрный охотник", "mineral_bloom", FishingHabitat.CaveLake, FishingEnemyBehavior.SoundHunt, NPCID.Piranha, 3, 390, 34, 16, 36, 26, ItemID.Topaz, "salvage_fragment", new Color(221, 210, 195)),
		new FishingEnemySpec("River Jetfish", "Речная струйница", "flash_flood", FishingHabitat.UndergroundRiver, FishingEnemyBehavior.RiverJet, NPCID.FlyingFish, 1, 160, 22, 5, 36, 24, ItemID.Trout, "river_amber", new Color(137, 195, 216)),
		new FishingEnemySpec("Silt Crawler", "Илистый ползун", "silt_run", FishingHabitat.UndergroundRiver, FishingEnemyBehavior.SiltHop, NPCID.SeaSnail, 4, 490, 38, 24, 34, 30, ItemID.SiltBlock, "silt_essence", new Color(178, 153, 108), BuffID.Slow)
	});
	public static int TypeAt(int index) => index switch
	{
		0 => ModContent.NPCType<ShoalSnapper>(), 1 => ModContent.NPCType<TideBell>(),
		2 => ModContent.NPCType<ReefCharger>(), 3 => ModContent.NPCType<StormSquid>(),
		4 => ModContent.NPCType<LanternMaw>(), 5 => ModContent.NPCType<PressureEel>(),
		6 => ModContent.NPCType<BlackwaterStalker>(), 7 => ModContent.NPCType<WakeCarapace>(),
		8 => ModContent.NPCType<CrystalUrchin>(), 9 => ModContent.NPCType<BlindLakeHunter>(),
		10 => ModContent.NPCType<RiverJetfish>(), 11 => ModContent.NPCType<SiltCrawler>(), _ => 0
	};
}
