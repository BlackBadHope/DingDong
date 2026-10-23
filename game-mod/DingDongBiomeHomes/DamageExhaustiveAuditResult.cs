using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
internal readonly record struct DamageExhaustiveAuditResult(int Items, int Projectiles, int HostileNpcs, int DamageOverTimeEffects, long ItemPairs, long ProjectilePairs, long DamageOverTimePairs, int Failures, string FailureExamples, ulong Digest, long ElapsedMilliseconds)
{
	public long ExpectedItemPairs => (long)Items * HostileNpcs;
	public long ExpectedProjectilePairs => (long)Projectiles * HostileNpcs;
	public long ExpectedDamageOverTimePairs => (long)DamageOverTimeEffects * HostileNpcs;

	public string ToMarker() => string.Join('|', new[] { "DINGDONG_DAMAGE_EXHAUSTIVE", $"verdict={(Failures == 0 && ItemPairs == ExpectedItemPairs && ProjectilePairs == ExpectedProjectilePairs && DamageOverTimePairs == ExpectedDamageOverTimePairs ? "PASS" : "FAIL")}", $"items={Items}", $"projectiles={Projectiles}", $"hostileNpcs={HostileNpcs}", $"dot={DamageOverTimeEffects}", $"itemPairs={ItemPairs}/{ExpectedItemPairs}", $"projectilePairs={ProjectilePairs}/{ExpectedProjectilePairs}", $"dotPairs={DamageOverTimePairs}/{ExpectedDamageOverTimePairs}", $"failures={Failures}", $"failureExamples={Sanitize(FailureExamples)}", $"digest={Digest:X16}", $"elapsedMs={ElapsedMilliseconds}", "worldMutated=False" });
	private static string Sanitize(string value) => string.IsNullOrWhiteSpace(value) ? "none" : value.Replace('\r', ' ').Replace('\n', ' ').Replace('|', '/');
}
