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
	private static List<SourceEntry> ClassifyDamageOverTime(IReadOnlyList<int> buffIds, AuditFailures failures)
	{
		List<SourceEntry> result = new(buffIds.Count);
		foreach (int buffId in buffIds)
		{
			string key = $"Buff/{buffId}";
			try
			{
				NPC carrier = new();
				carrier.buffType[0] = buffId;
				DamageSourceClassification first = DamageRuntimeClassifier.ClassifyDamageOverTime(carrier);
				DamageSourceClassification repeated = DamageRuntimeClassifier.ClassifyDamageOverTime(carrier);
				ValidateSourceClassification(first, repeated, $"dot:{key}", failures);
				result.Add(new SourceEntry(buffId, key, first, Fingerprint(first, buffId)));
			}
			catch (Exception exception)
			{
				failures.Add($"dot-classify:{key}:{ExceptionName(exception)}");
			}
		}

		return result;
	}
}
