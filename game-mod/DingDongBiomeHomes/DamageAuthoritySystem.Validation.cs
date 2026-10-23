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
	private static DamageReceiptReason ValidateAndPrepare(DamageHitIntent intent, int sender, out float serverMultiplier)
	{
		serverMultiplier = 1f;
		if (!Enabled || sender < 0 || sender >= Main.maxPlayers || !Main.player[sender].active)
			return DamageReceiptReason.InvalidSender;
		if (intent.TargetId < 0 || intent.TargetId >= Main.maxNPCs || !Main.npc[intent.TargetId].active)
			return DamageReceiptReason.InvalidTarget;
		Player player = Main.player[sender];
		NPC target = Main.npc[intent.TargetId];
		if (!DamageCombatPlayer.BelongsToPlayer(intent.AttackInstanceId, sender))
			return DamageReceiptReason.InvalidLineage;
		if (!float.IsFinite(intent.PredictedMultiplier) || intent.PredictedMultiplier <= 0f)
			return DamageReceiptReason.MultiplierMismatch;
		if (intent.ClientTick < 0 || (LatestClientTick.TryGetValue(sender, out long latestTick) && intent.ClientTick + 3 < latestTick))
		{
			return DamageReceiptReason.NonMonotonicTick;
		}

		DamageSourceClassification source;
		bool trueMultistrike;
		int expectedDamageType;
		Item heldItem = null;
		Projectile projectile = null;
		if (intent.SourceKind == DamageSourceKind.Item)
		{
			heldItem = player.HeldItem;
			if (heldItem == null || heldItem.IsAir || heldItem.type != intent.SourceType)
				return DamageReceiptReason.SourceMismatch;
			source = DamageRuntimeClassifier.Classify(heldItem);
			trueMultistrike = false;
			expectedDamageType = heldItem.DamageType.Type;
		}
		else if (intent.SourceKind == DamageSourceKind.Projectile)
		{
			projectile = FindProjectile(sender, intent.ProjectileIdentity, intent.SourceType);
			if (projectile == null)
				return DamageReceiptReason.SourceMismatch;
			DamageLineageGlobalProjectile lineage = projectile.GetGlobalProjectile<DamageLineageGlobalProjectile>();
			if (lineage.AttackInstanceId != intent.AttackInstanceId)
				return DamageReceiptReason.InvalidLineage;
			source = DamageRuntimeClassifier.Classify(projectile);
			trueMultistrike = source.TrueMultistrike;
			expectedDamageType = projectile.DamageType.Type;
			if (!lineage.MatchesHitEvent(projectile, trueMultistrike, intent.HitEventId))
				return DamageReceiptReason.SourceMismatch;
		}
		else
		{
			return DamageReceiptReason.SourceMismatch;
		}

		DamageTargetClassification targetClassification = DamageRuntimeClassifier.Classify(target);
		DamageHitLedger committedLedger = GetServerLedger(sender);
		DamageHitLedger tentativeLedger = committedLedger.Clone();
		DamageEvaluationTrace trace = ServerEngine.EvaluateAcceptedHit(source.Profile, targetClassification.Profile, new DamageHitObservation(intent.ClientTick, sender, targetClassification.EncounterRootId, intent.AttackInstanceId, intent.HitEventId, trueMultistrike), tentativeLedger, targetClassification.SegmentRole, targetClassification.PhaseRole);
		serverMultiplier = (float)trace.FinalMultiplier;
		if (Math.Abs(serverMultiplier - intent.PredictedMultiplier) > MultiplierTolerance)
			return DamageReceiptReason.MultiplierMismatch;
		DamagePayloadEnvelope payloadEnvelope;
		bool payloadAuthorized = heldItem != null
			? TryCreateItemPayloadEnvelope(player, target, heldItem, serverMultiplier, out payloadEnvelope)
			: TryCreateProjectilePayloadEnvelope(player, target, projectile, serverMultiplier, out payloadEnvelope);
		if (!payloadAuthorized)
			return DamageReceiptReason.SourceMismatch;
		DamageApprovalResult prepareResult = PendingApprovals.TryPrepare(sender, intent, tentativeLedger, serverMultiplier, (long)Main.GameUpdateCount, (long)Main.GameUpdateCount + ApprovalLifetimeTicks, expectedDamageType, payloadEnvelope);
		return prepareResult == DamageApprovalResult.Prepared ? DamageReceiptReason.Accepted : DamageReceiptReason.ServerBusy;
	}
}
