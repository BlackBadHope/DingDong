using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
/// <summary>
/// Data-driven runtime classifier. Optional-mod content is inspected only via
/// public Terraria/tModLoader metadata, so no compatibility assembly is a hard
/// dependency.
/// </summary>
public static partial class DamageRuntimeClassifier
{
	private static DamageSegmentRole InferSegmentRole(NPC npc)
	{
		string name = StableNpcIdentity(npc);
		if (ContainsAny(name, "head"))
			return DamageSegmentRole.Head;
		if (ContainsAny(name, "tail"))
			return DamageSegmentRole.Tail;
		if (ContainsAny(name, "core"))
			return DamageSegmentRole.Core;
		if (npc.realLife >= 0 || ContainsAny(name, "body"))
			return DamageSegmentRole.Body;
		return DamageSegmentRole.None;
	}

	private static DamagePhaseRole InferPhaseRole(NPC npc)
	{
		if (!npc.active || npc.lifeMax <= 0 || npc.life <= 0)
			return DamagePhaseRole.None;
		double ratio = (double)npc.life / npc.lifeMax;
		if (ratio <= 0.25d)
			return DamagePhaseRole.Critical;
		if (ratio <= 0.55d)
			return DamagePhaseRole.Wounded;
		return DamagePhaseRole.Opening;
	}

	private static bool IsSlime(NPC npc, string identity) => npc.aiStyle == NPCAIStyleID.Slime || ContainsAny(identity, "slime");
	private static string ItemKey(Item item) => $"dingdong:source/item/{SafeKey(item.ModItem?.FullName ?? item.type.ToString(CultureInfo.InvariantCulture))}";
	private static string ProjectileKey(Projectile projectile) => $"dingdong:source/projectile/{SafeKey(projectile.ModProjectile?.FullName ?? projectile.type.ToString(CultureInfo.InvariantCulture))}";
	private static string SafeKey(string value)
	{
		StringBuilder result = new(value.Length);
		foreach (char character in value.ToLowerInvariant())
			result.Append(char.IsLetterOrDigit(character) || character is '/' or '_' or '-' ? character : '_');
		return result.Length == 0 ? "unknown" : result.ToString();
	}

	private static bool ContainsAny(string value, params string[] fragments) => DamageNameMatcher.ContainsAny(value, fragments);
}
