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
	public static string InferElement(string displayName) => InferElementCore(displayName, legacy: false);

	// Exact old heuristic is retained only to recognize its saved defaults.
	internal static string InferLegacyElement(string displayName) => InferElementCore(displayName, legacy: true);

	private static string InferElementCore(string displayName, bool legacy)
	{
		string value = displayName ?? string.Empty;
		if (legacy) value = value.ToLowerInvariant();
		bool Has(params string[] words) => legacy
			? words.Any(word => value.Contains(word, StringComparison.Ordinal))
			: ContainsAny(value, words);
		if (Has("holy", "hallow", "radiant", "divine", "sanctum"))
			return DamageTags.Holy;
		if (Has("flame", "fire", "inferno", "hell", "solar", "sun", "cinder", "ember", "furnace", "hearth", "candle"))
			return DamageTags.Fire;
		if (Has("frost", "ice", "snow", "cryo", "blizzard"))
			return DamageTags.Frost;
		if (Has("lightning", "thunder", "electric", "shock", "storm"))
			return DamageTags.Lightning;
		if (Has("water", "aqua", "ocean", "tidal", "sea"))
			return DamageTags.Water;
		if (Has("earth", "rock", "stone", "sand", "terra"))
			return DamageTags.Earth;
		if (Has("poison", "venom", "toxic", "plague", "acid"))
			return DamageTags.Poison;
		if (Has("shadow", "dark", "night", "corrupt", "crimson", "blood"))
			return DamageTags.Shadow;
		if (Has("astral", "cosmic", "galaxy", "stellar", "star", "nebula"))
			return DamageTags.Astral;
		if (Has("void", "abyss", "null", "vortex"))
			return DamageTags.Void;
		if (Has("gel", "slime"))
			return DamageTags.Gel;
		if (Has("nature", "leaf", "vine", "bee", "finch", "bird", "briar"))
			return DamageTags.Nature;
		return DamageTags.Neutral;
	}

	internal static string StableItemIdentity(Item item) => item.ModItem?.FullName ?? ItemID.Search.GetName(item.type) ?? item.type.ToString(CultureInfo.InvariantCulture);
	internal static string StableProjectileIdentity(Projectile projectile) => projectile.ModProjectile?.FullName ?? ProjectileID.Search.GetName(projectile.type) ?? projectile.type.ToString(CultureInfo.InvariantCulture);
	private static string StableNpcIdentity(NPC npc) => npc.ModNPC?.FullName ?? NPCID.Search.GetName(npc.type) ?? npc.type.ToString(CultureInfo.InvariantCulture);
	private static string StableBuffIdentity(int type) => ModContent.GetModBuff(type)?.FullName ?? BuffID.Search.GetName(type) ?? type.ToString(CultureInfo.InvariantCulture);
	internal static IReadOnlyList<string> DescribeItemDeliveries(Item item)
	{
		List<string> tags = new()
		{
			InferItemDelivery(item)
		};
		if (item.shoot > ProjectileID.None && ContentSamples.ProjectilesByType.TryGetValue(item.shoot, out Projectile projectile))
		{
			string projectileDelivery = InferProjectileDelivery(projectile, null);
			if (!tags.Contains(projectileDelivery, StringComparer.Ordinal))
				tags.Add(projectileDelivery);
		}

		return tags;
	}

	internal static IReadOnlyList<string> DescribeItemBehaviors(Item item)
	{
		List<string> tags = new()
		{
			InferItemBehavior(item)
		};
		if (item.shoot > ProjectileID.None && ContentSamples.ProjectilesByType.TryGetValue(item.shoot, out Projectile projectile))
		{
			string projectileBehavior = InferProjectileBehavior(projectile);
			if (!tags.Contains(projectileBehavior, StringComparer.Ordinal))
				tags.Add(projectileBehavior);
		}

		return tags;
	}
}
