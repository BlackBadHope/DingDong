using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen;

internal static class NativeWorldgenStateSnapshot
{
	internal static object Capture()
	{
		Dictionary<string, object> state = new()
		{
			["purpose"] = "diagnostic globals; NOT tile checkpoint or general resume state",
			["width"] = Main.maxTilesX, ["height"] = Main.maxTilesY,
			["metadataSeed"] = Main.ActiveWorldFileData?.Seed,
			["generationSeed"] = typeof(WorldGen).GetField("_lastSeed", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(null),
			["surface"] = Main.worldSurface, ["rockLayer"] = Main.rockLayer,
			["ftw"] = WorldGen.getGoodWorldGen,
			["templeBounds"] = new[] { GenVars.tLeft, GenVars.tTop, GenVars.tRight, GenVars.tBottom },
			["vanillaAltar"] = new[] { GenVars.lAltarX, GenVars.lAltarY },
			["templeRooms"] = GenVars.tRooms,
			["mushroomBiomes"] = GenVars.numMushroomBiomes,
			["poiDensity"] = NativePoiDensityReceipt.Capture(),
			["underworldLayer"] = NativeUnderworldSystem.Layer,
			["expandedUnderworld"] = NativeUnderworldSystem.Active,
			["undergroundDesert"] = GenVars.UndergroundDesertLocation.ToString()
		};
		AddProvider(state, "CalamityMod", "CalamityMod.World.CustomTemple", "NewAlterPosition");
		AddProvider(state, "SOTS", "SOTS.WorldgenHelpers.AbandonedVillageWorldgenHelper", "AVRect", "OuterRect");
		AddProvider(state, "SOTS", "SOTS.WorldgenHelpers.PyramidWorldgenHelper", "placementLocation");
		return state;
	}

	private static void AddProvider(Dictionary<string, object> state, string modName, string typeName, params string[] fields)
	{
		if (!ModLoader.TryGetMod(modName, out Mod provider)) return;
		Type type = provider.Code.GetType(typeName);
		foreach (string name in fields)
		{
			var field = type?.GetField(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			state[modName + "." + name] = field?.GetValue(null)?.ToString() ?? "unavailable";
		}
	}
}
