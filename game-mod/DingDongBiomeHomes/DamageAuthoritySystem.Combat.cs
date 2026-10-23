using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
/// <summary>
/// Applies deterministic client prediction and validates the same decision on
/// the server before the vanilla DamageNPC packet is allowed to mutate life.
/// </summary>
public sealed partial class DamageAuthoritySystem
{
	internal static void ApplyItemHit(NPC target, Player player, Item item, ref NPC.HitModifiers modifiers)
	{
		if (!Enabled || Main.netMode == NetmodeID.Server || player.whoAmI != Main.myPlayer)
			return;
		ulong attackId = player.GetModPlayer<DamageCombatPlayer>().EnsureAttack();
		long tick = (long)Main.GameUpdateCount;
		DamageSourceClassification source = DamageRuntimeClassifier.Classify(item);
		DamageTargetClassification targetClassification = DamageRuntimeClassifier.Classify(target);
		DamageEvaluationTrace trace = ClientEngine.EvaluateAcceptedHit(source.Profile, targetClassification.Profile, new DamageHitObservation(tick, player.whoAmI, targetClassification.EncounterRootId, attackId, 1, false), ClientLedger, targetClassification.SegmentRole, targetClassification.PhaseRole);
		modifiers.FinalDamage *= (float)trace.FinalMultiplier;
		DamageFeedbackSystem.Report(target, source.Profile, targetClassification.Profile, trace);
		if (Main.netMode == NetmodeID.MultiplayerClient && StrictValidation)
		{
			SendPreparedIntent(new DamageHitIntent(DamageSourceKind.Item, target.whoAmI, item.type, -1, attackId, 1, tick, (float)trace.FinalMultiplier));
		}
	}
}
