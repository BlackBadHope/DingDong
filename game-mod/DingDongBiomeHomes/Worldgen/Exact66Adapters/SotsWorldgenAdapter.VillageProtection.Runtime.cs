using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using DingDongBiomeHomes.Worldgen.Compiler;
using DingDongBiomeHomes.Worldgen.Compiler.Runtime;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static partial class SotsWorldgenAdapter
{
	private static WeakReference<WorldCompilerPlan> villagePlan;
	private static WorldCompilerSpatialIndex villageProtected;
	private static SotsVillageSite villageSite;
	private static bool villageSelected;
	private static long villageConversionSkips, villageAmbientSkips;

	private static bool CompilerVillageActive()
	{
		WorldCompilerPlan plan = WorldCompilerRuntimeState.Current;
		if (!ContinentalAdapterMath.Active() || plan == null) return false;
		if (villagePlan == null || !villagePlan.TryGetTarget(out WorldCompilerPlan prior) || !ReferenceEquals(prior, plan))
		{
			villagePlan = new(plan);
			villageProtected = WorldCompilerProtectedGeometry.Create(plan.Surface, plan.Structures);
			villageSelected = false;
			villageConversionSkips = villageAmbientSkips = 0;
		}
		return true;
	}

	private static bool CompilerVillageSelected() => CompilerVillageActive() && villageSelected;

	private static bool TrySelectVillage(ref int x, ref int y, int width, int height)
	{
		if (!CompilerVillageActive()) return false;
		if (corruptionsField.GetValue(null) is not IList entries || entries.Count != 1)
			throw new InvalidOperationException("SOTS compiler village requires its single admitted evil envelope.");
		Rectangle evil = (Rectangle)corruptionRectangleField.GetValue(entries[0]);
		WorldCompilerPlan plan = WorldCompilerRuntimeState.Current;
		villageSite = SotsVillagePlacement.Find(x, y, width, height,
			new(evil.X, evil.Y, evil.Width, evil.Height), plan.Width, plan.Height,
			villageProtected.Overlaps, area => villageNativeValid(ToNative(area), 0) ||
				villageNativeValid(ToNative(area), 50),
			column => FindVillageGround(column, evil));
		x = villageSite.X;
		y = villageSite.Y;
		villageOuterField.SetValue(null, ToNative(villageSite.Outer));
		villageRectField.SetValue(null, new Rectangle(x - width / 2, y - height / 2, width, height));
		villageSelected = true;
		global::DingDongBiomeHomes.DingDongBiomeHomes.Instance.Logger.Info(
			$"DINGDONG_SOTS_VILLAGE_SITE|verdict=ADMITTED|center={x},{y}|size={width},{height}" +
			$"|sweep={villageSite.Sweep.X},{villageSite.Sweep.Y},{villageSite.Sweep.Width},{villageSite.Sweep.Height}" +
			$"|access={villageSite.Access.X},{villageSite.Access.Y},{villageSite.Access.Width},{villageSite.Access.Height}|ground={villageSite.Ground}|nativeGenerators=True");
		return true;
	}

	private static Rectangle ToNative(WorldCompilerRect area) => new(area.X, area.Y, area.Width, area.Height);

	private static bool AdmitVillageConversion(int x, int y)
	{
		if (!CompilerVillageActive() || !villageProtected.Overlaps(new(x - 1, y - 1, 3, 3))) return true;
		villageConversionSkips++;
		return false;
	}

	private static bool AdmitVillageAmbient(int x, int y)
	{
		// Covers forced furniture placements, their supports and neighbour framing.
		if (!CompilerVillageActive() || !villageProtected.Overlaps(new(x - 5, y - 5, 11, 9))) return true;
		villageAmbientSkips++;
		return false;
	}

	private static void ResetVillageProtection()
	{
		villagePlan = null;
		villageProtected = null;
		villageNativeValid = null;
		villageOuterField = villageRectField = null;
		villageGrassField = villageStoneField = null;
		villageSelected = false;
	}

	private static void LogVillageProtection()
	{
		if (!CompilerVillageSelected()) return;
		global::DingDongBiomeHomes.DingDongBiomeHomes.Instance.Logger.Info(
			$"DINGDONG_SOTS_VILLAGE_PROTECTION|verdict=COMPLETED|center={villageSite.X},{villageSite.Y}" +
			$"|conversionCellsProtected={villageConversionSkips}|ambientCandidatesProtected={villageAmbientSkips}|postRepair=False");
	}

}
