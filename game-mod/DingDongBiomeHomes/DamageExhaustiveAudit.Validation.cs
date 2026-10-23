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
	private static void ValidateSourceClassification(DamageSourceClassification first, DamageSourceClassification repeated, string contentKey, AuditFailures failures)
	{
		if (first.Coverage == DamageCoverageKind.Unknown)
			failures.Add($"source-unknown:{contentKey}");
		if (!SameSourceClassification(first, repeated))
			failures.Add($"source-nondeterministic:{contentKey}");
	}

	private static void ValidateTargetClassification(DamageTargetClassification first, DamageTargetClassification repeated, string contentKey, AuditFailures failures)
	{
		if (first.Coverage == DamageCoverageKind.Unknown)
			failures.Add($"target-unknown:{contentKey}");
		if (!SameTargetClassification(first, repeated))
			failures.Add($"target-nondeterministic:{contentKey}");
	}

	private static void ValidateTrace(SourceEntry source, TargetEntry target, DamageEvaluationTrace trace, AuditFailures failures)
	{
		bool finitePositive = IsFinitePositive(trace.DeliveryMultiplier) && IsFinitePositive(trace.ElementMultiplier) && IsFinitePositive(trace.BehaviorMultiplier) && IsFinitePositive(trace.AcquisitionMultiplier) && IsFinitePositive(trace.SegmentMultiplier) && IsFinitePositive(trace.PhaseMultiplier) && IsFinitePositive(trace.RawMultiplier) && IsFinitePositive(trace.FinalMultiplier);
		if (!finitePositive)
			failures.Add($"nonfinite:{source.ContentKey}->{target.ContentKey}");
		DamageTargetProfile profile = target.Classification.Profile;
		if (trace.FinalMultiplier < profile.MinimumMultiplier - Epsilon || trace.FinalMultiplier > profile.MaximumMultiplier + Epsilon || trace.FinalMultiplier < 0.80d - Epsilon || trace.FinalMultiplier > 1.30d + Epsilon)
		{
			failures.Add($"unclamped:{source.ContentKey}->{target.ContentKey}");
		}

		if (!string.Equals(trace.SourceKey, source.Classification.Profile.Key, StringComparison.Ordinal) || !string.Equals(trace.TargetKey, profile.Key, StringComparison.Ordinal))
		{
			failures.Add($"trace-key:{source.ContentKey}->{target.ContentKey}");
		}

		bool expectedClamped = Math.Abs(trace.RawMultiplier - trace.FinalMultiplier) > 0.0000001d;
		if (trace.WasClamped != expectedClamped)
			failures.Add($"clamp-trace:{source.ContentKey}->{target.ContentKey}");
		if (!trace.AcceptedIndependentEvent || !string.Equals(trace.ConditionalRule, "none", StringComparison.Ordinal))
			failures.Add($"first-hit-state:{source.ContentKey}->{target.ContentKey}");
	}
}
