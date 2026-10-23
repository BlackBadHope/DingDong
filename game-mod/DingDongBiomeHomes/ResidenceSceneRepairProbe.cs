using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.IO;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

internal static class ResidenceSceneRepairProbe
{
	// Bounded repair of the two screenshot-confirmed damaged scenes, on the
	// driver's COPY only. Never auto-repair a player's world on load.
	internal static void Run(Mod mod)
	{
		string path = Main.ActiveWorldFileData.Path;
		if (!Main.dedServ || Environment.GetEnvironmentVariable("DINGDONG_HOUSING_PROBE") != "1" ||
			!Path.GetFileName(Path.GetDirectoryName(path)).StartsWith("terrain-main-", StringComparison.Ordinal) ||
			!Path.GetFileName(path).StartsWith("DingDongTerrainProbe-", StringComparison.Ordinal))
			throw new InvalidOperationException("Scene repair requires the driver-owned copy.");
		var targets = SettlementWorldState.Residences.Where(r => r.PresetId is "Aegis8_HallowRidgeVillage" or "Aegis8_SnowCastle").ToArray();
		if (targets.Length != 2) throw new InvalidOperationException("Both screenshot scene identities are required");
		foreach (var target in targets) ValidateSafety(mod, target);
		bool wasGenerating = WorldGen.gen;
		try
		{
			WorldGen.gen = true;
			foreach (var target in targets)
			{
				NpcAuthoredPresetManifest.TryGetCandidate(target.NpcKey, out AuthoredPreset preset);
				AuthoredCandidateDeploymentManifest.TryGet(target.NpcKey, out var profile);
				DingDongAuthoredStructureLoader.Generate(mod, preset, target.Area.Location, profile);
				var housing = AuthoredSettlementBuilder.ProbeHousing(target.Area);
				DingDongAuthoredStructureLoader.RequireIntegrity(mod, preset, target.Area.Location, profile, "repair-post-framing");
				if (housing.ValidRooms == 0) throw new InvalidOperationException("Repaired residence lost housing: " + preset.Id);
				DingDongAuthoredStructureLoader.ExportSceneComparison(mod, target, Path.Combine(Path.GetDirectoryName(path), $"repaired-{target.ReviewId}.bmp"));
				mod.Logger.Info($"DINGDONG_SCENE_REPAIR_HOME|preset={preset.Id}|rooms={housing.ValidRooms}|area={target.Area}");
			}
			Main.worldName = "DingDong X10 - repaired structures r319";
			Main.ActiveWorldFileData.Name = Main.worldName;
			WorldFile.SaveWorld(false, false);
			mod.Logger.Info("DINGDONG_SCENE_REPAIR|verdict=PASS|repaired=2|savedCopy=" + path);
		}
		finally { WorldGen.gen = wasGenerating; }
	}
	private static void ValidateSafety(Mod mod, SettlementResidenceReport target)
	{
		Rectangle halo = target.Area; halo.Inflate(8, 8);
		foreach (var other in SettlementWorldState.Residences)
			if (other.ReviewId != target.ReviewId && halo.Intersects(other.Area))
				throw new InvalidOperationException($"Repair would approach residence {other.ReviewId}");
		foreach (var anchor in TileEntity.ByPosition.Keys)
			if (halo.Contains(anchor.X, anchor.Y)) throw new InvalidOperationException("Repair would touch tile entity " + anchor);
		foreach (var chest in Main.chest)
			if (chest != null && halo.Contains(chest.x, chest.y)) throw new InvalidOperationException($"Repair would touch chest {chest.x},{chest.y}");
		foreach (var sign in Main.sign)
			if (sign != null && halo.Contains(sign.x, sign.y)) throw new InvalidOperationException($"Repair would touch sign {sign.x},{sign.y}");
	}
}
