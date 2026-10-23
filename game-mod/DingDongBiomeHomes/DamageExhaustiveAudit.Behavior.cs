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
	public static DamageExhaustiveAuditResult Run(DamageRuleEngine engine)
	{
		ArgumentNullException.ThrowIfNull(engine);
		Stopwatch stopwatch = Stopwatch.StartNew();
		AuditFailures failures = new();
		KeyValuePair<int, Item>[] itemSamples = ContentSamples.ItemsByType.Where(pair => pair.Value.damage > 0 || pair.Value.shoot > ProjectileID.None).OrderBy(pair => pair.Key).ToArray();
		KeyValuePair<int, Projectile>[] projectileSamples = ContentSamples.ProjectilesByType.OrderBy(pair => pair.Key).ToArray();
		KeyValuePair<int, NPC>[] targetSamples = ContentSamples.NpcsByNetId.Where(pair => !pair.Value.friendly && !pair.Value.townNPC && pair.Value.lifeMax > 5).OrderBy(pair => pair.Key).ToArray();
		int[] damageOverTimeIds = Enumerable.Range(0, Main.debuff.Length).Where(index => Main.debuff[index]).ToArray();
		List<SourceEntry> itemSources = ClassifyItems(itemSamples, failures);
		List<SourceEntry> projectileSources = ClassifyProjectiles(projectileSamples, failures);
		List<SourceEntry> damageOverTimeSources = ClassifyDamageOverTime(damageOverTimeIds, failures);
		List<TargetEntry> targets = ClassifyTargets(targetSamples, failures);
		ulong digest = DigestSeed;
		long itemPairs = EvaluatePairs(engine, itemSources, targets, 1UL, failures, ref digest);
		long projectilePairs = EvaluatePairs(engine, projectileSources, targets, 2UL, failures, ref digest);
		long damageOverTimePairs = EvaluatePairs(engine, damageOverTimeSources, targets, 3UL, failures, ref digest);
		stopwatch.Stop();
		return new DamageExhaustiveAuditResult(itemSamples.Length, projectileSamples.Length, targetSamples.Length, damageOverTimeIds.Length, itemPairs, projectilePairs, damageOverTimePairs, failures.Count, string.Join(',', failures.Examples), digest, stopwatch.ElapsedMilliseconds);
	}

	private static List<SourceEntry> ClassifyItems(IReadOnlyList<KeyValuePair<int, Item>> samples, AuditFailures failures)
	{
		List<SourceEntry> result = new(samples.Count);
		foreach ((int contentId, Item item)in samples)
		{
			string key = item.ModItem?.FullName ?? $"Terraria/Item/{contentId}";
			try
			{
				DamageSourceClassification first = DamageRuntimeClassifier.Classify(item);
				DamageSourceClassification repeated = DamageRuntimeClassifier.Classify(item);
				ValidateSourceClassification(first, repeated, $"item:{key}", failures);
				result.Add(new SourceEntry(contentId, key, first, Fingerprint(first, contentId)));
			}
			catch (Exception exception)
			{
				failures.Add($"item-classify:{key}:{ExceptionName(exception)}");
			}
		}

		return result;
	}
}
