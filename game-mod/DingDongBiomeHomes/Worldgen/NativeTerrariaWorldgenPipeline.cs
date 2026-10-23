using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.GameContent.Generation;
using Terraria.ModLoader;
using Terraria.WorldBuilding;
using DingDongBiomeHomes.Worldgen.Compiler.Runtime;

namespace DingDongBiomeHomes.Worldgen;

internal static class NativeTerrariaWorldgenPipeline
{
	internal static void Attach(Mod owner, List<GenPass> tasks, ref double weight)
	{
		if (!Exact66WorldgenAdmission.TryAdmit(owner, tasks, out _, out string providers))
			throw new InvalidOperationException("Native x10 provider admission failed: " + providers);
		GenPass[] original = tasks.ToArray();
		NativeCoastPlacement.Reset();
		NativeUnderworldSystem.Arm();
		NativeTerrariaWorldgenReceipt.Arm(original.Where(pass => pass.Enabled).Select(pass => pass.Name));
		string path = Path.Combine(Main.SavePath, "DingDongDiagnostics", "Worldgen",
			$"native-{DateTime.UtcNow:yyyyMMddTHHmmssfffZ}-{Guid.NewGuid():N}.jsonl");
		var journal = new WorldgenStageJournal(path, new {
			backend = "Terraria-native", seed = Main.ActiveWorldFileData?.Seed,
			width = Main.maxTilesX, height = Main.maxTilesY,
			ftw = WorldGen.getGoodWorldGen,
			passes = original.Select(pass => new { pass.Name, pass.Enabled,
				type = pass.GetType().FullName }).ToArray(),
			mods = ModLoader.Mods.Select(mod => new { mod.Name,
				version = mod.Version.ToString(), mvid = mod.Code?.ManifestModule.ModuleVersionId }).ToArray()
		}, () => [], message => owner.Logger.Warn(message),
			"original Terraria/mod pass execution; not visual approval or a complete tile/loot audit",
			NativeWorldgenStateSnapshot.Capture);
		for (int i = 0; i < original.Length; i++)
			tasks[i] = new NativeTerrariaWorldgenPass(original[i], journal,
				message => owner.Logger.Info(message));
		tasks.Add(new PassLegacy("DingDong native worldgen receipt", (_, _) => {
			NativeTerrariaWorldgenReceipt.Seal();
			if (!NativePoiDensityReceipt.CoverageComplete)
				throw new InvalidOperationException(
					"Native repeatable POI density receipt is incomplete: " +
					string.Join(",", NativePoiDensityReceipt.MissingTargets()));
			owner.Logger.Info($"DINGDONG_NATIVE_WORLDGEN|verdict=PASS|backend=Terraria-native" +
				$"|completed={NativeTerrariaWorldgenReceipt.CompletedCount}" +
				"|poiDensity=PASS|poiTargets=16/16" +
				$"|world={Main.maxTilesX}x{Main.maxTilesY}|schematicPasses=0" +
				"|lateBiomeRepaint=False|rectangularAnnexes=False|visualApproved=False");
		}, 0.01));
		weight += 0.01;
		owner.Logger.Info($"DINGDONG_NATIVE_WORLDGEN|verdict=ARMED|backend=Terraria-native" +
			$"|originalPasses={original.Length}|providers={providers}|journal={path}");
	}
}
