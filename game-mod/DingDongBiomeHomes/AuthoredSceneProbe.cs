using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

internal static class AuthoredSceneProbe
{
	// Isolated fixture only; never saves. Test real decoding/framing for every asset
	// before paying for a whole-world placement or regenerating native terrain.
	internal static void Run(Mod mod)
	{
		if (!Main.dedServ || Environment.GetEnvironmentVariable("DINGDONG_HOUSING_PROBE") != "1" ||
			!Path.GetFileName(Main.ActiveWorldFileData.Path).StartsWith("DingDongTerrainProbe-", StringComparison.Ordinal))
			throw new InvalidOperationException("Authored scene probe requires an isolated diagnostic world.");
		Point origin = new(1000, 50);
		List<string> failures = new();
		int tested = 0;
		bool wasGenerating = WorldGen.gen;
		string only = Environment.GetEnvironmentVariable("DINGDONG_SCENE_PROBE_PRESETS");
		try
		{
			WorldGen.gen = Environment.GetEnvironmentVariable("DINGDONG_SCENE_PROBE_GEN_FALSE") != "1";
			foreach (var profile in AuthoredCandidateDeploymentManifest.All)
			{
				NpcAuthoredPresetManifest.TryGetCandidate(profile.NpcKey, out AuthoredPreset preset);
				if (!string.IsNullOrEmpty(only) && !Array.Exists(only.Split(';'), id => id == preset.Id)) continue;
				Rectangle area = new(origin.X, origin.Y, preset.Width, preset.Height);
				Rectangle halo = area; halo.Inflate(8, 8);
				AuthoredTileSnapshot[] before = new AuthoredTileSnapshot[halo.Width * halo.Height];
				for (int x = 0; x < halo.Width; x++)
				for (int y = 0; y < halo.Height; y++)
				{
					Tile tile = Main.tile[halo.X + x, halo.Y + y];
					before[x * halo.Height + y] = AuthoredTileSnapshot.Read(tile);
					tile.ClearEverything();
				}
				var tileZero = AuthoredTileSnapshot.Read(Main.tile[0, 0]);
				Chest[] priorContainers = (Chest[])Main.chest.Clone();
				using var containers = AuthoredNativeContainerTransaction.Begin();
				try
				{
					DingDongAuthoredStructureLoader.InspectSceneMask(mod, preset);
					DingDongAuthoredStructureLoader.InspectMutationMask(mod, preset, profile);
					if (tileZero != AuthoredTileSnapshot.Read(Main.tile[0, 0]))
						throw new InvalidOperationException("Read-only loader changed tile zero");
					DingDongAuthoredStructureLoader.Generate(mod, preset, origin, profile);
					var liquidsBefore = Liquids(area);
					DingDongAuthoredStructureLoader.RequireIntegrity(mod, preset, origin, profile, "probe-after-import-framing");
					var housing = AuthoredSettlementBuilder.ProbeHousing(area);
					var liquidsAfter = Liquids(area);
					mod.Logger.Info($"DINGDONG_SCENE_PROBE_LIQUID|preset={preset.Id}|afterImport={liquidsBefore}|afterHousingQuery={liquidsAfter}|ftw={Main.getGoodWorld}|worldGen={WorldGen.gen}");
					DingDongAuthoredStructureLoader.RequireIntegrity(mod, preset, origin, profile, "probe-after-read-only-housing");
					DingDongAuthoredStructureLoader.RequireNativeContainer(preset, origin, requireEmpty: true);
					DingDongAuthoredStructureLoader.ProbeRestoredContext(mod, preset, profile, origin);
					AuthoredSourceAmbientPolicy.ProbeDefaultOff(mod, preset, origin);
					if (housing.ValidRooms == 0) throw new InvalidOperationException("No habitable rooms: " + housing.FirstFailure);
					tested++;
					mod.Logger.Info($"DINGDONG_SCENE_PROBE_HOME|verdict=PASS|preset={preset.Id}|rooms={housing.ValidRooms}|tileZeroPreserved=True");
				}
				catch (Exception ex) { failures.Add(preset.Id + ":" + ex.Message); }
				finally
				{
					for (int x = 0; x < halo.Width; x++)
					for (int y = 0; y < halo.Height; y++)
						before[x * halo.Height + y].Restore(Main.tile[halo.X + x, halo.Y + y]);
					containers.Dispose();
					for (int slot = 0; slot < priorContainers.Length; slot++)
						if (!ReferenceEquals(priorContainers[slot], Main.chest[slot]))
							throw new InvalidOperationException("Scene probe did not restore native container ownership.");
				}
			}
		}
		finally { WorldGen.gen = wasGenerating; }
		foreach (string failure in failures) mod.Logger.Error("DINGDONG_SCENE_PROBE_FAILURE|" + failure);
		mod.Logger.Info($"DINGDONG_SCENE_PROBE|passed={tested}|failed={failures.Count}|worldSaved=False|visualApproval=False");
		if (failures.Count != 0) throw new InvalidOperationException("Scene probe failures=" + failures.Count);
	}
	private static string Liquids(Rectangle area)
	{
		int water = 0, lava = 0, honey = 0, shimmer = 0;
		for (int x = area.Left; x < area.Right; x++)
		for (int y = area.Top; y < area.Bottom; y++)
		{
			Tile tile = Main.tile[x, y];
			if (tile.LiquidAmount == 0) continue;
			switch (tile.LiquidType) { case 0: water++; break; case 1: lava++; break; case 2: honey++; break; case 3: shimmer++; break; }
		}
		return $"water:{water},lava:{lava},honey:{honey},shimmer:{shimmer}";
	}
}
