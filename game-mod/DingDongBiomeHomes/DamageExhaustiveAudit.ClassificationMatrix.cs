using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
/// <summary>
/// Dedicated-only exhaustive compatibility gate. Every loaded damage-capable
/// item, projectile and debuff source is evaluated against every loaded
/// hostile NPC target through the production rule engine. The audit never
/// mutates world, player, item or NPC persistence state.
/// </summary>
internal static partial class DamageExhaustiveAudit
{
	private static List<SourceEntry> ClassifyProjectiles(IReadOnlyList<KeyValuePair<int, Projectile>> samples, AuditFailures failures)
	{
		List<SourceEntry> result = new(samples.Count);
		foreach ((int contentId, Projectile projectile)in samples)
		{
			string key = projectile.ModProjectile?.FullName ?? $"Terraria/Projectile/{contentId}";
			try
			{
				DamageSourceClassification first = DamageRuntimeClassifier.Classify(projectile);
				DamageSourceClassification repeated = DamageRuntimeClassifier.Classify(projectile);
				ValidateSourceClassification(first, repeated, $"projectile:{key}", failures);
				result.Add(new SourceEntry(contentId, key, first, Fingerprint(first, contentId)));
			}
			catch (Exception exception)
			{
				failures.Add($"projectile-classify:{key}:{ExceptionName(exception)}");
			}
		}

		return result;
	}

	private static List<TargetEntry> ClassifyTargets(IReadOnlyList<KeyValuePair<int, NPC>> samples, AuditFailures failures)
	{
		List<TargetEntry> result = new(samples.Count);
		foreach ((int contentId, NPC npc)in samples)
		{
			string key = npc.ModNPC?.FullName ?? $"Terraria/NPC/{contentId}";
			try
			{
				DamageTargetClassification first = DamageRuntimeClassifier.Classify(npc);
				DamageTargetClassification repeated = DamageRuntimeClassifier.Classify(npc);
				ValidateTargetClassification(first, repeated, key, failures);
				result.Add(new TargetEntry(contentId, key, first, Fingerprint(first, contentId)));
			}
			catch (Exception exception)
			{
				failures.Add($"target-classify:{key}:{ExceptionName(exception)}");
			}
		}

		return result;
	}

	private static long EvaluatePairs(DamageRuleEngine engine, IReadOnlyList<SourceEntry> sources, IReadOnlyList<TargetEntry> targets, ulong matrixKind, AuditFailures failures, ref ulong digest)
	{
		long evaluated = 0;
		for (int sourceIndex = 0; sourceIndex < sources.Count; sourceIndex++)
		{
			SourceEntry source = sources[sourceIndex];
			DamageHitLedger ledger = new();
			for (int targetIndex = 0; targetIndex < targets.Count; targetIndex++)
			{
				TargetEntry target = targets[targetIndex];
				try
				{
					ulong lineage = ((ulong)(sourceIndex + 1) << 32) | (uint)(targetIndex + 1);
					DamageEvaluationTrace trace = engine.EvaluateAcceptedHit(source.Classification.Profile, target.Classification.Profile, new DamageHitObservation(targetIndex * 4L, 0, 1, lineage, 1, source.Classification.TrueMultistrike), ledger, target.Classification.SegmentRole, target.Classification.PhaseRole);
					evaluated++;
					ValidateTrace(source, target, trace, failures);
					digest = Mix(digest, matrixKind);
					digest = Mix(digest, source.Fingerprint);
					digest = Mix(digest, target.Fingerprint);
					digest = Mix(digest, unchecked((ulong)BitConverter.DoubleToInt64Bits(trace.RawMultiplier)));
					digest = Mix(digest, unchecked((ulong)BitConverter.DoubleToInt64Bits(trace.FinalMultiplier)));
				}
				catch (Exception exception)
				{
					failures.Add($"pair:{source.ContentKey}->{target.ContentKey}:{ExceptionName(exception)}");
				}
			}
		}

		return evaluated;
	}
}
