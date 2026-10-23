using System;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

/// <summary>
/// Applies the same ontology to aggregate NPC damage-over-time and lava life
/// degeneration. This hook is deterministic on server and clients; the server
/// remains the authoritative NPC-life owner in multiplayer.
/// </summary>
public sealed class DamageOverTimeGlobalNPC : GlobalNPC
{
	private static readonly DamageRuleEngine Engine = new();
	private static readonly DamageHitLedger Ledger = new();

	public override void UpdateLifeRegen(NPC npc, ref int damage)
	{
		if (!DamageAuthoritySystem.IsEnabled || npc.friendly || npc.townNPC || npc.lifeRegen >= 0)
			return;

		DamageSourceClassification source = DamageRuntimeClassifier.ClassifyDamageOverTime(npc);
		DamageTargetClassification target = DamageRuntimeClassifier.Classify(npc);
		ulong attackId = 0xfe00000000000000ul | (uint)(target.EncounterRootId + 1);
		DamageEvaluationTrace trace = Engine.EvaluateAcceptedHit(
			source.Profile,
			target.Profile,
			new DamageHitObservation((long)Main.GameUpdateCount, Main.maxPlayers + 1, target.EncounterRootId, attackId, 1, false),
			Ledger,
			target.SegmentRole,
			target.PhaseRole);

		double multiplier = trace.FinalMultiplier;
		if (Math.Abs(multiplier - 1d) <= 0.000001d)
			return;

		npc.lifeRegen = Math.Min(-1, (int)Math.Round(npc.lifeRegen * multiplier, MidpointRounding.AwayFromZero));
		if (damage > 0)
			damage = Math.Max(1, (int)Math.Round(damage * multiplier, MidpointRounding.AwayFromZero));
	}
}
