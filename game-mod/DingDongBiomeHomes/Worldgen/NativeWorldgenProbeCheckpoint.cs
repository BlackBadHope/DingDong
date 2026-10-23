using System;
using System.IO;
using System.Text.Json;
using Terraria;
using Terraria.IO;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen;

internal static class NativeWorldgenProbeCheckpoint
{
	internal static void Save(Mod mod)
	{
		string path = Main.ActiveWorldFileData?.Path;
		if (!Main.dedServ || Environment.GetEnvironmentVariable("DINGDONG_WORLDGEN_CHECKPOINT") != "1" ||
			Environment.GetEnvironmentVariable("DINGDONG_HEIGHT_COMPAT_PROBE") != "1" || path == null ||
			!Path.GetFileName(path).StartsWith("DingDongTerrainProbe-",StringComparison.Ordinal)) return;
		FileMetadata before = Main.WorldFileMetadata;
		try
		{
			// Normal metadata is created after GenerateWorld returns. This fixture
			// snapshot is earlier; the native header writer requires this object.
			Main.WorldFileMetadata ??= FileMetadata.FromCurrentSettings(FileType.World);
			WorldFile.SaveWorld();
			string target = Path.ChangeExtension(path,"pre-homes.wld");
			File.Copy(path,target,false);
			File.Copy(Path.ChangeExtension(path,"twld"),Path.ChangeExtension(target,"twld"),false);
			File.WriteAllText(Path.ChangeExtension(target,"receipt.json"),JsonSerializer.Serialize(new {
				stage = "before-authored-residences", world = target, completeGeneration = false,
				worldBytes = new FileInfo(target).Length, originalSavesUsed = false }));
			mod.Logger.Info($"DINGDONG_HEIGHT_WORLDGEN_CHECKPOINT|stage=before-residences|completeGeneration=False|path={target}");
		}
		catch (Exception error) { mod.Logger.Warn("DINGDONG_HEIGHT_WORLDGEN_CHECKPOINT|status=UNAVAILABLE|worldgenContinues=True",error); }
		finally { Main.WorldFileMetadata = before; }
	}
}
