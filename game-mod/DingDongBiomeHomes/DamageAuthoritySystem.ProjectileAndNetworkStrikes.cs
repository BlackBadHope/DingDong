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
	internal static void ApplyProjectileHit(NPC target, Projectile projectile, ref NPC.HitModifiers modifiers)
	{
		if (!Enabled)
			return;
		DamageLineageGlobalProjectile lineage = projectile.GetGlobalProjectile<DamageLineageGlobalProjectile>();
		DamageSourceClassification source = DamageRuntimeClassifier.Classify(projectile);
		DamageTargetClassification targetClassification = DamageRuntimeClassifier.Classify(target);
		if (Main.netMode == NetmodeID.Server)
		{
			if (projectile.owner >= 0 && projectile.owner < Main.maxPlayers)
				return;
			ulong serverAttackId = lineage.EnsureServerAttackInstance(serverAllocator);
			uint serverEvent = lineage.HitEventId(projectile, source.TrueMultistrike);
			DamageEvaluationTrace serverTrace = ServerEngine.EvaluateAcceptedHit(source.Profile, targetClassification.Profile, new DamageHitObservation((long)Main.GameUpdateCount, Main.maxPlayers, targetClassification.EncounterRootId, serverAttackId, serverEvent, source.TrueMultistrike), ServerWorldLedger, targetClassification.SegmentRole, targetClassification.PhaseRole);
			modifiers.FinalDamage *= (float)serverTrace.FinalMultiplier;
			return;
		}

		if (projectile.owner != Main.myPlayer)
			return;
		Player owner = Main.player[projectile.owner];
		ulong attackId = lineage.AttackInstanceId != 0 ? lineage.AttackInstanceId : owner.GetModPlayer<DamageCombatPlayer>().EnsureAttack();
		long tick = (long)Main.GameUpdateCount;
		uint eventId = lineage.HitEventId(projectile, source.TrueMultistrike);
		DamageEvaluationTrace trace = ClientEngine.EvaluateAcceptedHit(source.Profile, targetClassification.Profile, new DamageHitObservation(tick, owner.whoAmI, targetClassification.EncounterRootId, attackId, eventId, source.TrueMultistrike), ClientLedger, targetClassification.SegmentRole, targetClassification.PhaseRole);
		modifiers.FinalDamage *= (float)trace.FinalMultiplier;
		DamageFeedbackSystem.Report(target, source.Profile, targetClassification.Profile, trace);
		if (Main.netMode == NetmodeID.MultiplayerClient && StrictValidation)
		{
			SendPreparedIntent(new DamageHitIntent(DamageSourceKind.Projectile, target.whoAmI, projectile.type, projectile.identity, attackId, eventId, tick, (float)trace.FinalMultiplier));
		}
	}

	private static void CaptureOutgoingStrike(On_NetMessage.orig_SendStrikeNPC original, NPC npc, ref NPC.HitInfo hit, int ignoreClient)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient && Enabled && StrictValidation)
			TryCommitOutgoingStrike(npc, hit, (long)Main.GameUpdateCount);
		original(npc, ref hit, ignoreClient);
	}

	private static void TryCommitOutgoingStrike(NPC npc, in NPC.HitInfo hit, long clientTick)
	{
		if (!ClientPendingCommits.Remove(npc.whoAmI, out ClientPendingCommit pending) || pending.ExpiresAtTick < clientTick)
		{
			return;
		}

		DamageHitIntent intent = pending.Intent;
		SendCommit(new DamageHitCommit(intent.SourceKind, intent.TargetId, intent.SourceType, intent.ProjectileIdentity, intent.AttackInstanceId, intent.HitEventId, DamageStrikePayload.FromHit(hit)));
	}
}
