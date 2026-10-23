using System;
using System.Collections.Generic;
using System.Reflection;
using MonoMod.RuntimeDetour;
using DingDongBiomeHomes.Fishing.Worldgen;
using Terraria;
using Terraria.ModLoader;
using Terraria.WorldBuilding;
using Microsoft.Xna.Framework;

namespace DingDongBiomeHomes.Worldgen;

// Keep provider algorithms and local structure sizes. Change only their world
// envelope contract: reserve the future coastline before Thorium selects land.
internal sealed class NativeCoastalSystem : ModSystem
{
	private readonly List<ILHook> hooks = new();
	private static Func<int> seaWidth;
	private bool required, installed;
	internal static bool Active => WorldGen.gen && ContinentalWorldDimensions.IsContinental &&
		!Compiler.Runtime.WorldCompilerActivation.IsActive;
	internal static int ScanStart(int value) => Clip(value, true);
	internal static int ScanEnd(int value) => Clip(value, false);
	private static int Clip(int value, bool lower)
	{
		int width = Active ? seaWidth?.Invoke() ?? 0 : 0;
		// The sea's final width is chosen later. Reserve its expanded envelope
		// now, while Thorium still has a chance to choose a land location.
		if (FishingWaterWorldSystem.Generating)
			width = FishingWaterGeometry.OceanWidth(Main.maxTilesX, width);
		return NativeCoastalPolicy.ClipScan(value, lower,
			Main.maxTilesX, Main.dungeonX, width, Active);
	}
	internal static int ScaleClearance(int value) => NativeCoastalPolicy.Clearance(value, Main.maxTilesY, Active);

	public override void PostSetupContent()
	{
		if (!ModLoader.TryGetMod("CalamityMod", out var calamity)) return;
		required = true;
		try
		{
			Type sea = calamity.Code.GetType("CalamityMod.World.SulphurousSea", true);
			hooks.Add(new ILHook(typeof(WorldGen).GetMethod(nameof(WorldGen.AddBuriedChest),
				new[] { typeof(int), typeof(int), typeof(int), typeof(bool), typeof(int), typeof(bool), typeof(ushort) }),
				NativeCoastPlacementPatches.BuriedChest));
			hooks.Add(new ILHook(typeof(StructureMap).GetMethod(nameof(StructureMap.CanPlace),
				new[] { typeof(Rectangle), typeof(bool[]), typeof(int) }), NativeCoastPlacementPatches.CanPlace));
			seaWidth = sea.GetProperty("BiomeWidth", BindingFlags.Public | BindingFlags.Static)?.GetMethod.CreateDelegate<Func<int>>()
				?? throw new MissingMemberException("SulphurousSea.BiomeWidth");
			hooks.Add(new ILHook(sea.GetMethod("RemoveStupidTilesAboveSea"), il => NativeCoastalPatches.AirClearance(il, 140)));
			hooks.Add(new ILHook(sea.GetMethod("GenerateShallowTopWater"), il => NativeCoastalPatches.AirClearance(il, 150)));
			if (ModLoader.TryGetMod("ThoriumMod", out var thorium))
			{
				Type grave = thorium.Code.GetType("ThoriumMod.WorldGeneration.Graveyard.GraveyardPass", true);
				hooks.Add(new ILHook(grave.GetMethod("GetRangeForBiome"), NativeCoastalPatches.GraveyardRange));
			}
			installed = true;
			Mod.Logger.Info("DINGDONG_COASTAL|hooks=installed|scope=new-native-x10|standardWorlds=unchanged");
		}
		catch (Exception error)
		{
			DisposeHooks();
			Mod.Logger.Warn("DINGDONG_COASTAL|hooks=unsupported|" + error);
		}
	}
	public override void PreWorldGen()
	{
		if (ContinentalWorldDimensions.IsContinental && required && !installed)
			throw new InvalidOperationException("Cannot generate x10: coastal compatibility hooks unavailable.");
	}
	public override void Unload() { DisposeHooks(); seaWidth = null; required = false; NativeCoastPlacement.Reset(); }
	private void DisposeHooks()
	{
		for (int i = hooks.Count - 1; i >= 0; i--) hooks[i].Dispose();
		hooks.Clear(); installed = false;
	}
}
