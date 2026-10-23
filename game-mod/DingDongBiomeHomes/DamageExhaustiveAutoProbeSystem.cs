using System;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

/// <summary>
/// Headless full-modpack entry point for the exhaustive damage matrix. This is
/// deliberately inert in normal play and exists because some Windows hosts do
/// not expose a Win32 console buffer to tModLoader's command loop.
/// </summary>
public sealed class DamageExhaustiveAutoProbeSystem : ModSystem
{
	private const string EnvironmentVariable = "DINGDONG_DAMAGE_EXHAUSTIVE_AUTO";
	private bool ran;

	public override void PostSetupContent()
	{
		if (ran
			|| !Main.dedServ
			|| !string.Equals(Environment.GetEnvironmentVariable(EnvironmentVariable), "1", StringComparison.Ordinal))
		{
			return;
		}

		ran = true;
		try
		{
			DamageCoverageSnapshot coverage = DamageRuntimeClassifier.AuditLoadedContent();
			DamageExhaustiveAuditResult exhaustive = DamageExhaustiveAudit.Run(new DamageRuleEngine());
			bool countsMatch = exhaustive.Items == coverage.Items
				&& exhaustive.Projectiles == coverage.Projectiles
				&& exhaustive.HostileNpcs == coverage.HostileNpcs
				&& exhaustive.DamageOverTimeEffects == coverage.DamageOverTimeEffects;
			bool pairsComplete = exhaustive.ItemPairs == exhaustive.ExpectedItemPairs
				&& exhaustive.ProjectilePairs == exhaustive.ExpectedProjectilePairs
				&& exhaustive.DamageOverTimePairs == exhaustive.ExpectedDamageOverTimePairs;
			string verdict = coverage.Unknown == 0 && countsMatch && pairsComplete && exhaustive.Failures == 0
				? "PASS"
				: "FAIL";

			Mod.Logger.Info(coverage.ToMarker());
			Mod.Logger.Info(exhaustive.ToMarker());
			Mod.Logger.Info(
				$"DINGDONG_DAMAGE_EXHAUSTIVE_AUTOPROBE|verdict={verdict}|countsMatch={countsMatch}|pairsComplete={pairsComplete}|unknown={coverage.Unknown}|failures={exhaustive.Failures}|worldMutated=False");
		}
		catch (Exception exception)
		{
			Mod.Logger.Error(
				$"DINGDONG_DAMAGE_EXHAUSTIVE_AUTOPROBE|verdict=FAIL|exception={exception.GetType().Name}|worldMutated=False",
				exception);
		}
	}

	public override void Unload() => ran = false;
}
