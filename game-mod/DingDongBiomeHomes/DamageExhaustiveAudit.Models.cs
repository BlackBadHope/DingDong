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
	private readonly record struct SourceEntry(int ContentId, string ContentKey, DamageSourceClassification Classification, ulong Fingerprint);
	private readonly record struct TargetEntry(int ContentId, string ContentKey, DamageTargetClassification Classification, ulong Fingerprint);
	private sealed class AuditFailures
	{
		private const int MaximumExamples = 24;
		private readonly List<string> examples = new();
		public int Count { get; private set; }
		public IReadOnlyList<string> Examples => examples;

		public void Add(string failure)
		{
			Count++;
			if (examples.Count < MaximumExamples)
				examples.Add(failure);
		}
	}
}
