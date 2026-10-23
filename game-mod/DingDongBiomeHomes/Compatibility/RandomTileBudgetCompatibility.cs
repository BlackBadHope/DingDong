using System;
using System.Collections.Generic;
using System.Reflection;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Compatibility;

/// <summary>Bounds aggregate random growth work on oversized maps; ordinary worlds retain native operands.</summary>
public sealed class RandomTileBudgetCompatibility : ModSystem
{
	private readonly List<ILHook> hooks = new();
	private static readonly Guid EngineMvid = new("033e87cd-cefe-4dc6-a1fb-e14aa938bbb2");
	private static readonly Guid CalamityMvid = new("0e63c698-ec72-446e-ac1f-8d01f0ee7ac7");

	public override void Load()
	{
		try
		{
			MethodInfo vanilla = typeof(WorldGen).GetMethod("UpdateWorld_Inner",
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
				null, Type.EmptyTypes, null) ?? throw new MissingMethodException("WorldGen.UpdateWorld_Inner");
			if (vanilla.Module.ModuleVersionId != EngineMvid)
				throw new InvalidOperationException("Unsupported engine identity for random tile budget.");
			Type calamity = null;
			if (ModLoader.TryGetMod("CalamityMod", out Mod provider))
			{
				calamity = provider.Code.GetType("CalamityMod.Systems.WorldMiscUpdateSystem", throwOnError: true);
				if (calamity.Module.ModuleVersionId != CalamityMvid)
					throw new InvalidOperationException("Unsupported Calamity identity for random tile budget.");
			}
			hooks.Add(new ILHook(vanilla, RandomTileBudgetPatches.Vanilla));
			if (calamity != null)
				hooks.Add(new ILHook(R19CompatibilitySystem.Method(calamity, "HandleTileGrowth"), RandomTileBudgetPatches.Calamity));
			Mod.Logger.Info($"DINGDONG_RANDOM_TILE_BUDGET|status=INSTALLED|areaCap={RandomTileBudgetPolicy.VanillaLargeArea}|vanillaSites=3|calamitySites={(calamity != null ? 4 : 0)}|serverSetting=RandomTileBudgetConfig.AutoNormalizeExpandedWorldGrowth|coordinateDomainUnchanged=True|standardWorldOperandsUnchanged=True");
		}
		catch (Exception exception)
		{
			// Optional performance adaptation: unsupported IL restores original work
			// instead of leaving one provider patched or failing game loading.
			bool clean = DisposeHooks();
			Mod.Logger.Warn($"DINGDONG_RANDOM_TILE_BUDGET|status={(clean ? "NOT_INSTALLED" : "CLEANUP_FAILED")}|reason={exception.Message}");
		}
	}

	public override void OnWorldLoad()
	{
		if (hooks.Count == 0) return;
		long area = (long)Main.maxTilesX * Main.maxTilesY;
		if (area > RandomTileBudgetPolicy.VanillaLargeArea)
		{
			bool enabled = ModContent.GetInstance<RandomTileBudgetConfig>().AutoNormalizeExpandedWorldGrowth;
			Mod.Logger.Info($"DINGDONG_RANDOM_TILE_BUDGET|status={(enabled ? "ACTIVE" : "DISABLED_BY_SERVER")}|physicalArea={area}|effectiveArea={(enabled ? RandomTileBudgetPolicy.VanillaLargeArea : area)}|perTileRateRatio={(enabled ? RandomTileBudgetPolicy.VanillaLargeArea / (double)area : 1d):R}|fullCoordinateDomain=True|liquidsUnchanged=True");
		}
	}

	public override void Unload() => DisposeHooks();

	private bool DisposeHooks()
	{
		bool clean = true;
		for (int i = hooks.Count - 1; i >= 0; i--)
		{
			try { hooks[i].Dispose(); }
			catch (Exception exception)
			{
				clean = false;
				Mod.Logger.Warn($"DINGDONG_RANDOM_TILE_BUDGET|status=CLEANUP_FAILED|reason={exception.Message}");
			}
		}
		hooks.Clear();
		return clean;
	}
}
