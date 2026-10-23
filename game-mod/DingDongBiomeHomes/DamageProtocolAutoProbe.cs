using System;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

/// <summary>
/// Disposable-harness entry point for the complete damage authority probe. It
/// is inert in normal play and restricted to an explicit test environment or
/// the exact disposable harness naming contract.
/// </summary>
internal static class DamageProtocolAutoProbe
{
	private const string EnvironmentVariable = "DINGDONG_DAMAGE_PROTOCOL_AUTO";
	private static bool ran;

	internal static void TryRun(Mod mod)
	{
		bool explicitEnvironment = string.Equals(
			Environment.GetEnvironmentVariable(EnvironmentVariable),
			"1",
			StringComparison.Ordinal);
		bool disposableHarness = Main.SavePath?.IndexOf(
			"DingDongDamageGuideFullpack-",
			StringComparison.OrdinalIgnoreCase) >= 0
			|| Array.Exists(
				Environment.GetCommandLineArgs(),
				argument => argument.EndsWith("serverconfig-damage-guide.txt", StringComparison.OrdinalIgnoreCase));
		if (ran
			|| (!explicitEnvironment && !disposableHarness))
		{
			return;
		}

		ran = true;
		const string worldFile = "preworld-fullpack";
		string trigger = explicitEnvironment ? "environment" : "disposableHarness";

		try
		{
			string summary = DingDongDamageProbeCommand.RunProbe(mod, worldFile);
			bool passed = summary.Contains("|verdict=PASS|", StringComparison.Ordinal);
			mod.Logger.Info(
				$"DINGDONG_DAMAGE_PROTOCOL_AUTOPROBE|verdict={(passed ? "PASS" : "FAIL")}|trigger={trigger}|world={OneLine(worldFile)}|worldMutated=False");
		}
		catch (Exception exception)
		{
			mod.Logger.Error(
				$"DINGDONG_DAMAGE_PROTOCOL_AUTOPROBE|verdict=FAIL|trigger={trigger}|exception={exception.GetType().Name}|world={OneLine(worldFile)}|worldMutated=False",
				exception);
		}
	}

	internal static void Reset() => ran = false;

	private static string OneLine(string value) => string.IsNullOrWhiteSpace(value)
		? "none"
		: value.Replace('\r', ' ').Replace('\n', ' ').Replace('|', '/');
}
