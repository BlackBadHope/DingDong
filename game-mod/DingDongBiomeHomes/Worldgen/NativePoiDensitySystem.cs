using System;
using System.Collections.Generic;
using System.Reflection;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.ModLoader;
using Terraria.Utilities;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen;

internal sealed class NativePoiDensitySystem : ModSystem
{
	private readonly List<ILHook> hooks = new();
	internal static string HookStatus { get; private set; } = "not-loaded";
	internal static bool Active => ContinentalWorldDimensions.IsContinental &&
		ModContent.GetInstance<DingDongServerConfig>().EnableContinentalWorlds &&
		!Compiler.Runtime.WorldCompilerActivation.IsActive;

	public override void Load()
	{
		try
		{
			Install("<AddGenPasses>b__288_47", NativePoiDensityPatches.Jungle);
			Install("<AddGenPasses>b__288_19", NativePoiDensityPatches.Sky);
			Install("<AddGenPasses>b__288_21", il =>
				NativePoiDensityPatches.Range(il, "Count", "marble"));
			Install("<AddGenPasses>b__288_22", il =>
				NativePoiDensityPatches.Range(il, "Count", "granite"));
			Install("<AddGenPasses>b__288_58", il => {
				NativePoiDensityPatches.Range(il, "CaveHouseCount", "cave-houses");
				NativePoiDensityPatches.Range(il, "UnderworldChestCount", "underworld-chests");
				NativePoiDensityPatches.Range(il, "CaveChestCount", "cave-chests");
				NativePoiDensityPatches.Range(il, "AdditionalDesertHouseCount", "desert-houses");
			});
			Install("<AddGenPasses>b__288_100", il => {
				NativePoiDensityPatches.Range(il, "DeadManChests", "trapped-chests");
				NativePoiDensityPatches.Range(il, "SwordShrineAttempts", "sword-shrines");
				NativePoiDensityPatches.Range(il, "ThinIcePatchCount", "thin-ice");
				NativePoiDensityPatches.Range(il, "CampsiteCount", "campsites");
				NativePoiDensityPatches.Range(il, "ExplosiveTrapCount", "explosive-traps");
				NativePoiDensityPatches.Range(il, "LivingTreeCount", "living-trees");
				NativePoiDensityPatches.Range(il, "LongTrackCount", "long-tracks");
				NativePoiDensityPatches.Range(il, "StandardTrackCount", "standard-tracks");
			});
			HookStatus = "installed";
		}
		catch (Exception error)
		{
			DisposeHooks();
			HookStatus = "unsupported-kernel: " + error.Message;
			Mod.Logger.Warn("DINGDONG_POI_DENSITY|state=DISABLED|reason=" + error);
		}
	}

	public override void PreWorldGen()
	{
		NativePoiDensityReceipt.Reset();
		if (Active) Mod.Logger.Info($"DINGDONG_POI_DENSITY|hooks={HookStatus}" +
			"|policy=Large-continuation|localGeometry=unchanged|uniqueStructures=unchanged");
	}

	public override void Unload()
	{
		DisposeHooks();
		HookStatus = "not-loaded";
		NativePoiDensityReceipt.Reset();
	}

	private void Install(string name, ILContext.Manipulator patch)
	{
		Type closure = typeof(WorldGen).GetNestedType("<>c", BindingFlags.NonPublic)
			?? throw new MissingMemberException("WorldGen.<>c");
		MethodInfo method = closure.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)
			?? throw new MissingMethodException(closure.FullName, name);
		hooks.Add(new ILHook(method, patch));
	}

	private void DisposeHooks()
	{
		for (int i = hooks.Count - 1; i >= 0; i--) hooks[i].Dispose();
		hooks.Clear();
	}

	internal static double JungleTarget(double native)
	{
		double target = NativePoiDensityPolicy.Underground(native, Main.maxTilesY, Active);
		if (Active) NativePoiDensityReceipt.Record("jungle-huts", native, target,
			"WorldWidth+Depth");
		return target;
	}

	internal static int SkyTarget(int native)
	{
		int target = NativePoiDensityPolicy.SkyLakes(native, Main.maxTilesX, Active);
		if (Active) NativePoiDensityReceipt.Record("sky-lakes", native, target,
			"WorldWidthSurface");
		return target;
	}

	internal static int RangeTarget(WorldGenRange range, UnifiedRandom random, string kind)
	{
		// Retain the provider's draw and explicit None/WorldArea overrides.
		int native = range.GetRandom(random);
		bool horizontal = kind == "long-tracks";
		int target = NativePoiDensityPolicy.UndergroundCount(native, Main.maxTilesY,
			Active, range.ScaleWith == WorldGenRange.ScalingMode.WorldWidth, horizontal);
		if (Active) NativePoiDensityReceipt.Record(kind, native, target,
			horizontal ? "WorldWidth-horizontal" : range.ScaleWith.ToString());
		return target;
	}
}
