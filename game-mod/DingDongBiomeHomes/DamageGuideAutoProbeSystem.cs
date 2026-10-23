using System;
using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

/// <summary>
/// Optional full-pack acceptance hook. Unlike the pre-world protocol probe,
/// this verifies world-dependent difficulty getters only after the disposable
/// world has finished loading. It is inert during normal play.
/// </summary>
public sealed class DamageGuideAutoProbeSystem : ModSystem
{
	private const string EnvironmentVariable = "DINGDONG_DAMAGE_GUIDE_WORLD_AUTO";
	private const string DisposableWorldPrefix = "DingDongDamageProbe-Guide-";
	private bool ran;

	public override void PostWorldLoad()
	{
		string worldPath = Main.ActiveWorldFileData?.Path ?? string.Empty;
		string worldFile = Path.GetFileNameWithoutExtension(worldPath);
		bool explicitlyRequested = string.Equals(
			Environment.GetEnvironmentVariable(EnvironmentVariable),
			"1",
			StringComparison.Ordinal);
		bool disposableGuideWorld = worldFile.StartsWith(DisposableWorldPrefix, StringComparison.Ordinal);
		bool dedicatedGuideConfig = Array.Exists(
			Environment.GetCommandLineArgs(),
			argument => argument.EndsWith("serverconfig-damage-guide.txt", StringComparison.OrdinalIgnoreCase));
		if (ran || !Main.dedServ || (!explicitlyRequested && !disposableGuideWorld && !dedicatedGuideConfig))
		{
			return;
		}

		ran = true;
		try
		{
			DamageDifficultyContextSystem.MarkWorldReady();
			string summary = DingDongDamageProbeCommand.RunProbe(Mod, worldFile);
			DamageDifficultySnapshot difficulty = DamageDifficultyContextSystem.Capture();
			bool legendary = Main.masterMode && Main.getGoodWorld;
			bool passed = summary.Contains("|verdict=PASS|", StringComparison.Ordinal)
				&& difficulty.ExternalModes.Count == DamageDifficultyContextSystem.AdapterCount
				&& difficulty.AdapterFailures == 0;

			Mod.Logger.Info(
				$"DINGDONG_DAMAGE_GUIDE_WORLD_AUTOPROBE|verdict={(passed ? "PASS" : "FAIL")}|world={worldFile}|masterMode={Main.masterMode}|forTheWorthy={Main.getGoodWorld}|legendary={legendary}|externalAdapters={difficulty.ExternalModes.Count}|adapterFailures={difficulty.AdapterFailures}|worldMutated=False");
		}
		catch (Exception exception)
		{
			Mod.Logger.Error(
				$"DINGDONG_DAMAGE_GUIDE_WORLD_AUTOPROBE|verdict=FAIL|exception={exception.GetType().Name}|worldMutated=False",
				exception);
		}
	}

	public override void OnWorldUnload() => ran = false;
	public override void Unload() => ran = false;
}
