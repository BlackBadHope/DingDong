using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if !DINGDONG_UNDERWORLD_TESTS
using MonoMod.RuntimeDetour;
using Terraria;
#endif

namespace DingDongBiomeHomes.Worldgen;

internal static class NativeUnderworldLayerConsumers
{
	// Exact tML 2026.7.3.0 call-site census, checked against the installed IL in
	// NativeUnderworldKernelChecks. Includes gameplay, generation and rendering.
	internal static readonly (string Type, string Method, int Calls)[] Manifest = {
		("Terraria.Liquid", "Update", 1),
		("Terraria.Main", "UpdateAudio_DecideOnTOWMusic", 3),
		("Terraria.Main", "UpdateAudio_DecideOnNewMusic", 3),
		("Terraria.Main", "DrawUnderworldBackgroudLayer", 1),
		("Terraria.Main", "DrawBackground", 3),
		("Terraria.Main", "DrawBlack", 3),
		("Terraria.NPC", "VanillaAI_Inner", 5),
		("Terraria.NPC", "SpawnNPC", 10),
		("Terraria.NPC", "SpawnWOF", 1),
		("Terraria.Player", "UpdateBuffs", 1),
		("Terraria.Player", "UpdateBiomes", 4),
		("Terraria.Player", "WOFTongue", 1),
		("Terraria.Player", "Update", 1),
		("Terraria.Player", "BiomeTorchPlaceStyle", 1),
		("Terraria.Player", "BiomeTorchHoldStyle", 1),
		("Terraria.Player", "BiomeCampfirePlaceStyle", 1),
		("Terraria.Player", "BiomeCampfireHoldStyle", 1),
		("Terraria.Player", "GetPickaxeDamage", 1),
		("Terraria.Player", "DemonConch", 1),
		("Terraria.Player", "TeleportationPotion", 1),
		("Terraria.Projectile", "CanExplodeTile", 1),
		("Terraria.WorldGen", "GeneratePlanteraBulbOnAllMechsDefeated", 1),
		("Terraria.WorldGen", "PlaceSandTrap", 1),
		("Terraria.WorldGen", "MakeDungeon_GroundFurniture", 1),
		("Terraria.WorldGen", "HellFort", 2),
		("Terraria.WorldGen", "KillTile", 1),
		("Terraria.WorldGen", "plantDye", 1),
		("Terraria.WorldGen", "SpawnThingsFromPot", 5),
		("Terraria.WorldGen+<>c", "<AddGenPasses>b__288_14", 2),
		("Terraria.WorldGen+<>c", "<AddGenPasses>b__288_58", 1),
		("Terraria.WorldGen+<>c", "<AddGenPasses>b__288_61", 4),
		("Terraria.WorldGen+<>c", "<AddGenPasses>b__288_64", 1),
		("Terraria.WorldGen+<>c", "<AddGenPasses>b__288_70", 1),
		("Terraria.WorldGen+<>c", "<AddGenPasses>b__288_83", 1),
		("Terraria.WorldGen+<>c", "<AddGenPasses>b__288_101", 1),
		("Terraria.Map.MapHelper", "CreateMapTile", 1),
		("Terraria.Map.MapHelper", "LoadMapVersion1", 3),
		("Terraria.Graphics.Light.TileLightScanner", "GetTileLight", 1),
		("Terraria.GameContent.Skies.AmbientSky+HellBatsGoupSkyEntity", ".ctor", 1),
		("Terraria.GameContent.Skies.AmbientSky+HellBatsGoupSkyEntity", "CreateGroup", 1),
		("Terraria.GameContent.ItemDropRules.Conditions+LivingFlames", "CanDrop", 1),
		("Terraria.GameContent.Generation.TrackGenerator", "IsLocationInvalid", 1),
		("Terraria.GameContent.Drawing.WallDrawing", "DrawWalls", 1),
		("Terraria.GameContent.Biomes.JunglePass", "GenerateHolesInMudWalls", 2),
		("Terraria.GameContent.Ambience.AmbienceServer", "IsPlayerInAPlaceWhereTheyCanSeeAmbienceHell", 1)
	};

#if !DINGDONG_UNDERWORLD_TESTS
	internal static void Install(List<ILHook> hooks)
	{
		const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
		foreach (var target in Manifest)
		{
			Type type = typeof(Main).Assembly.GetType(target.Type, throwOnError: true);
			MethodBase[] matches = target.Method == ".ctor" ? type.GetConstructors(flags).Cast<MethodBase>().ToArray() :
				type.GetMethods(flags).Where(m => m.Name == target.Method).Cast<MethodBase>().ToArray();
			if (matches.Length != 1) throw new MissingMethodException(target.Type, target.Method);
			hooks.Add(new ILHook(matches[0], il => NativeUnderworldPatches.PatchLayerConsumer(il, target.Type, target.Method, target.Calls)));
		}
	}
#endif
}
