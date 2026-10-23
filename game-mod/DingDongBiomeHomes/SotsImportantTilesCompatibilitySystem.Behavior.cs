using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
/// <summary>
/// SOTS 0.25.1.9 queues a full Main.tile scan on ThreadPool. A client can join
/// while the server is still changing world/tile state, which makes that worker
/// observe an invalid Tilemap index and terminate the server. Keep SOTS' own
/// landmark objects and packet protocol, but perform the scan incrementally on
/// the main game thread.
/// </summary>
public sealed partial class SotsImportantTilesCompatibilitySystem
{
	public override void PostSetupContent()
	{
		instance = this;
		if (!ModLoader.TryGetMod("SOTS", out Mod sots))
		{
			Mod.Logger.Info("DINGDONG_SOTS_TILE_SCAN|verdict=SKIP|reason=SOTS-not-loaded");
			return;
		}

		if (!string.Equals(sots.Version.ToString(), VulnerableSotsVersion, StringComparison.Ordinal))
		{
			Mod.Logger.Info($"DINGDONG_SOTS_TILE_SCAN|verdict=SKIP|reason=version-not-targeted|version={sots.Version}");
			return;
		}

		Type worldType = sots.Code.GetType("SOTS.Common.Systems.ImportantTilesWorld", throwOnError: false);
		MethodInfo threadTileResetting = worldType?.GetMethod("ThreadTileResetting", BindingFlags.Public | BindingFlags.Static, binder: null, types: Type.EmptyTypes, modifiers: null);
		FieldInfo listField = worldType?.GetField("List", BindingFlags.NonPublic | BindingFlags.Static);
		finishedThreadingField = worldType?.GetField("finishedThreading", BindingFlags.Public | BindingFlags.Static);
		if (worldType == null || threadTileResetting == null || listField == null || finishedThreadingField == null)
			throw new InvalidOperationException("DingDong refused to arm the SOTS 0.25.1.9 tile-scan guard because the audited API contract changed.");
		if (listField.GetValue(null)is not IList sotsLandmarks || sotsLandmarks.Count == 0)
			throw new InvalidOperationException("DingDong refused to arm the SOTS 0.25.1.9 tile-scan guard because its landmark list is empty.");
		landmarks.Clear();
		foreach (object landmark in sotsLandmarks)
		{
			Type landmarkType = landmark.GetType();
			FieldInfo position = landmarkType.GetField("Position", BindingFlags.Public | BindingFlags.Instance);
			FieldInfo tileType = landmarkType.GetField("TileType", BindingFlags.Public | BindingFlags.Instance);
			MethodInfo assignPoint = landmarkType.GetMethod("AssignPoint", BindingFlags.Public | BindingFlags.Instance, binder: null, types: new[] { typeof(Tile), typeof(int), typeof(int), typeof(bool) }, modifiers: null);
			if (position == null || tileType == null || assignPoint == null)
				throw new InvalidOperationException($"DingDong found an unsupported SOTS landmark contract: {landmarkType.FullName}.");
			landmarks.Add(new LandmarkDescriptor(landmark, Convert.ToUInt16(tileType.GetValue(landmark)), position, assignPoint));
		}

		hookDelegate = DetourThreadTileResetting;
		MonoModHooks.Add(threadTileResetting, hookDelegate);
		Mod.Logger.Info($"DINGDONG_SOTS_TILE_SCAN|verdict=ARMED|version={sots.Version}|mode=main-thread-incremental|landmarks={landmarks.Count}|tilesPerTick={TilesPerTick}");
	}
}
