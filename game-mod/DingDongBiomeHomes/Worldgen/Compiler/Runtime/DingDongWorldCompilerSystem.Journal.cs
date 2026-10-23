using System;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.GameContent.Generation;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

public sealed partial class DingDongWorldCompilerSystem
{
	private static WorldgenStageJournal journal;
	private void StartJournal()
	{
		string path = Path.Combine(Main.SavePath, "DingDongDiagnostics", "Worldgen",
			$"{DateTime.UtcNow:yyyyMMddTHHmmssfffZ}-{Guid.NewGuid():N}.jsonl");
		journal = new WorldgenStageJournal(path, new {
			seed = Main.ActiveWorldFileData?.Seed, width = Main.maxTilesX, height = Main.maxTilesY,
			gameMode = Main.GameMode, ftw = WorldGen.getGoodWorldGen,
			mods = ModLoader.Mods.Select(m => new { m.Name, version = m.Version.ToString(),
				mvid = m.Code?.ManifestModule.ModuleVersionId.ToString() }).ToArray()
		}, WorldgenStageSnapshot.Read, message => Mod.Logger.Warn(message));
		Mod.Logger.Info($"DINGDONG_STAGE_JOURNAL|path={path}|format=jsonl|flushToDisk=True");
	}

	private static GenPass ObservePass(GenPass pass) =>
		new PassLegacy(pass.Name, (progress, config) =>
			journal.Run(pass.Name, () => pass.Apply(progress, config)), pass.Weight);
}
