using System;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.WeaponForge;

internal enum WeaponForgeTrait : byte
{
	// crafting station
	Woodcraft, Tempered, Hellforged, Mithril, HeavyForge, DarkRite, Celestial, Mechanism, Inscribed, Arcane, Infused, Masterwork,
	// dominant material
	Supple, Sturdy, Keen, Noble, Corrupted, Meteoric, Molten, Light, Fine, Dense, Blessed, Verdant, Ethereal, Carapace,
	StarMetal, Soulbound, Spectral, Gemcut, Ossified, Elastic, Predator, Alloy, Raw,
	// source
	BossTrophy, RareFind
}

internal enum WeaponForgeBiome : byte { None, Underworld, Snow, Jungle, Hallow, Evil, Ocean, Desert, Sky, Meteor, Dungeon, Forest }

internal static class WeaponForgeTraits
{
	// (crit, speed, knockback, projectile velocity, melee reach) of each trait.
	private static (int Crit, float Speed, float Knock, float Velocity, float Scale) Stats(WeaponForgeTrait trait) => trait switch
	{
		WeaponForgeTrait.Woodcraft => (0, 0.05f, 0f, 0f, 0f),
		WeaponForgeTrait.Tempered => (2, 0f, 0f, 0f, 0f),
		WeaponForgeTrait.Hellforged => (0, 0f, 0.10f, 0f, 0f),
		WeaponForgeTrait.Mithril => (4, 0f, 0f, 0f, 0f),
		WeaponForgeTrait.HeavyForge => (0, 0f, 0.08f, 0f, 0.05f),
		WeaponForgeTrait.DarkRite => (3, 0f, 0f, 0f, 0f),
		WeaponForgeTrait.Celestial => (0, 0.06f, 0f, 0f, 0f),
		WeaponForgeTrait.Mechanism => (0, 0.05f, 0f, 0f, 0f),
		WeaponForgeTrait.Inscribed => (0, 0f, 0f, 0.10f, 0f),
		WeaponForgeTrait.Arcane => (0, 0f, 0f, 0.08f, 0f),
		WeaponForgeTrait.Infused => (3, 0f, 0f, 0f, 0f),
		WeaponForgeTrait.Masterwork => (3, 0f, 0f, 0f, 0f),
		WeaponForgeTrait.Supple => (0, 0.04f, 0f, 0f, 0f),
		WeaponForgeTrait.Sturdy => (0, 0f, 0.06f, 0f, 0f),
		WeaponForgeTrait.Keen => (3, 0f, 0f, 0f, 0f),
		WeaponForgeTrait.Noble => (0, 0f, 0f, 0.05f, 0.03f),
		WeaponForgeTrait.Corrupted => (3, 0f, 0f, 0f, 0f),
		WeaponForgeTrait.Meteoric => (0, 0f, 0f, 0.08f, 0f),
		WeaponForgeTrait.Molten => (0, 0f, 0.08f, 0f, 0f),
		WeaponForgeTrait.Light => (0, 0.05f, 0f, 0f, 0f),
		WeaponForgeTrait.Fine => (3, 0f, 0f, 0f, 0f),
		WeaponForgeTrait.Dense => (0, 0f, 0.06f, 0f, 0.04f),
		WeaponForgeTrait.Blessed => (0, 0.05f, 0f, 0f, 0f),
		WeaponForgeTrait.Verdant => (0, 0f, 0f, 0.08f, 0f),
		WeaponForgeTrait.Ethereal => (4, 0f, 0f, 0f, 0f),
		WeaponForgeTrait.Carapace => (0, 0f, 0.10f, 0f, 0f),
		WeaponForgeTrait.StarMetal => (0, 0.06f, 0f, 0f, 0f),
		WeaponForgeTrait.Soulbound => (3, 0f, 0f, 0f, 0f),
		WeaponForgeTrait.Spectral => (0, 0f, 0f, 0.06f, 0f),
		WeaponForgeTrait.Gemcut => (0, 0f, 0f, 0.06f, 0f),
		WeaponForgeTrait.Ossified => (3, 0f, 0f, 0f, 0f),
		WeaponForgeTrait.Elastic => (0, 0f, 0.06f, 0f, 0f),
		WeaponForgeTrait.Predator => (3, 0f, 0f, 0f, 0f),
		WeaponForgeTrait.Alloy => (3, 0f, 0f, 0f, 0f),
		WeaponForgeTrait.Raw => (0, 0f, 0.05f, 0f, 0f),
		WeaponForgeTrait.BossTrophy => (3, 0f, 0f, 0f, 0f),
		WeaponForgeTrait.RareFind => (0, 0f, 0f, 0f, 0f), // its value is the damage share counted as effort
		_ => (0, 0f, 0f, 0f, 0f)
	};

	internal static WeaponForgeTrait? StationTrait(int tile)
	{
		switch (tile)
		{
			case TileID.WorkBenches: case TileID.Sawmill: case TileID.HeavyWorkBench: return WeaponForgeTrait.Woodcraft;
			case TileID.Anvils: return WeaponForgeTrait.Tempered;
			case TileID.Hellforge: return WeaponForgeTrait.Hellforged;
			case TileID.MythrilAnvil: return WeaponForgeTrait.Mithril;
			case TileID.AdamantiteForge: return WeaponForgeTrait.HeavyForge;
			case TileID.DemonAltar: return WeaponForgeTrait.DarkRite;
			case TileID.LunarCraftingStation: return WeaponForgeTrait.Celestial;
			case TileID.TinkerersWorkbench: return WeaponForgeTrait.Mechanism;
			case TileID.Bookcases: return WeaponForgeTrait.Inscribed;
			case TileID.CrystalBall: return WeaponForgeTrait.Arcane;
			case TileID.ImbuingStation: return WeaponForgeTrait.Infused;
			case TileID.Autohammer: return WeaponForgeTrait.Verdant;
		}
		if (tile >= TileID.Count && TileLoader.GetTile(tile) is ModTile modTile)
		{
			string name = modTile.Name;
			if (Has(name, "Anvil")) return WeaponForgeTrait.Tempered;
			if (Has(name, "Forge", "Furnace", "Crucible")) return WeaponForgeTrait.Hellforged;
			if (Has(name, "Altar", "Shrine", "Ritual")) return WeaponForgeTrait.DarkRite;
			if (Has(name, "Crystal", "Arcane", "Magic", "Book")) return WeaponForgeTrait.Arcane;
			return WeaponForgeTrait.Masterwork;
		}
		return null;
	}

	// Internal (language independent) names only, so server and clients derive the same trait.
	internal static WeaponForgeTrait? MaterialTrait(Item material)
	{
		string name = material.ModItem?.Name ?? ItemID.Search.GetName(material.type) ?? "";
		if (Has(name, "Luminite", "Fragment")) return WeaponForgeTrait.StarMetal;
		if (Has(name, "Chlorophyte")) return WeaponForgeTrait.Verdant;
		if (Has(name, "Shroomite", "Spectre")) return WeaponForgeTrait.Ethereal;
		if (Has(name, "Beetle")) return WeaponForgeTrait.Carapace;
		if (Has(name, "Hallowed")) return WeaponForgeTrait.Blessed;
		if (Has(name, "Adamantite", "Titanium")) return WeaponForgeTrait.Dense;
		if (Has(name, "Mythril", "Orichalcum")) return WeaponForgeTrait.Fine;
		if (Has(name, "Cobalt", "Palladium")) return WeaponForgeTrait.Light;
		if (Has(name, "Hellstone")) return WeaponForgeTrait.Molten;
		if (Has(name, "Meteorite")) return WeaponForgeTrait.Meteoric;
		if (Has(name, "Demonite", "Crimtane")) return WeaponForgeTrait.Corrupted;
		if (Has(name, "Gold", "Platinum")) return WeaponForgeTrait.Noble;
		if (Has(name, "Silver", "Tungsten")) return WeaponForgeTrait.Keen;
		if (Has(name, "Iron", "Lead")) return WeaponForgeTrait.Sturdy;
		if (Has(name, "Copper", "Tin")) return WeaponForgeTrait.Supple;
		if (Has(name, "Soul")) return WeaponForgeTrait.Soulbound;
		if (Has(name, "Ectoplasm")) return WeaponForgeTrait.Spectral;
		if (Has(name, "Amethyst", "Topaz", "Sapphire", "Emerald", "Ruby", "Diamond", "Amber", "Gem")) return WeaponForgeTrait.Gemcut;
		if (Has(name, "Wood")) return WeaponForgeTrait.Woodcraft;
		if (Has(name, "Bone")) return WeaponForgeTrait.Ossified;
		if (Has(name, "Gel")) return WeaponForgeTrait.Elastic;
		if (Has(name, "Scale", "Fang", "Tooth", "Claw", "Stinger", "Mandible")) return WeaponForgeTrait.Predator;
		if (Has(name, "Bar", "Ingot", "Alloy")) return WeaponForgeTrait.Alloy;
		if (Has(name, "Essence", "Core", "Shard", "Crystal")) return WeaponForgeTrait.Arcane;
		if (Has(name, "Ore")) return WeaponForgeTrait.Raw;
		return null;
	}

	internal static string ResolveElement(Item weapon, Recipe recipe)
	{
		string element = weapon.TryGetGlobalItem(out DamageAffinityGlobalItem affinity) ? affinity.ElementTag : DamageTags.Neutral;
		if (element != DamageTags.Neutral || recipe == null) return element;
		if (recipe.requiredTile.Contains(TileID.Hellforge)) return DamageTags.Fire;
		foreach (Item ingredient in recipe.requiredItem.Where(i => !i.IsAir).OrderByDescending(i => i.stack).ThenBy(i => i.type))
		{
			string inferred = DamageRuntimeClassifier.InferElement(DamageRuntimeClassifier.StableItemIdentity(ingredient));
			if (inferred != DamageTags.Neutral) return inferred;
		}
		return DamageTags.Neutral;
	}

	internal static WeaponForgeBiome BiomeOf(string element) => element switch
	{
		DamageTags.Fire => WeaponForgeBiome.Underworld,
		DamageTags.Frost => WeaponForgeBiome.Snow,
		DamageTags.Poison or DamageTags.Nature => WeaponForgeBiome.Jungle,
		DamageTags.Holy => WeaponForgeBiome.Hallow,
		DamageTags.Shadow => WeaponForgeBiome.Evil,
		DamageTags.Water => WeaponForgeBiome.Ocean,
		DamageTags.Earth => WeaponForgeBiome.Desert,
		DamageTags.Lightning => WeaponForgeBiome.Sky,
		DamageTags.Astral => WeaponForgeBiome.Meteor,
		DamageTags.Void => WeaponForgeBiome.Dungeon,
		DamageTags.Gel => WeaponForgeBiome.Forest,
		_ => WeaponForgeBiome.None
	};

	internal static bool InBiome(Player player, WeaponForgeBiome biome) => biome switch
	{
		WeaponForgeBiome.Underworld => player.ZoneUnderworldHeight,
		WeaponForgeBiome.Snow => player.ZoneSnow,
		WeaponForgeBiome.Jungle => player.ZoneJungle,
		WeaponForgeBiome.Hallow => player.ZoneHallow,
		WeaponForgeBiome.Evil => player.ZoneCorrupt || player.ZoneCrimson,
		WeaponForgeBiome.Ocean => player.ZoneBeach,
		WeaponForgeBiome.Desert => player.ZoneDesert || player.ZoneUndergroundDesert,
		WeaponForgeBiome.Sky => player.ZoneSkyHeight || (Main.raining && player.ZoneOverworldHeight),
		WeaponForgeBiome.Meteor => player.ZoneMeteor,
		WeaponForgeBiome.Dungeon => player.ZoneDungeon,
		WeaponForgeBiome.Forest => player.ZonePurity,
		_ => false
	};

	// Sums the traits into the profile's stat bonuses, each with its hard cap; reach only for
	// melee, projectile speed only for shooters, use speed and knockback not for minion staves.
	internal static void Apply(WeaponForgeProfile p, Item weapon)
	{
		int crit = 0; float speed = 0f, knock = 0f, velocity = 0f, scale = 0f;
		foreach (WeaponForgeTrait trait in p.Traits)
		{
			var s = Stats(trait);
			crit += s.Crit; speed += s.Speed; knock += s.Knock; velocity += s.Velocity; scale += s.Scale;
		}
		bool summon = p.Group == WeaponForgeGroup.Summon;
		p.CritBonus = summon ? 0 : Math.Min(crit, WeaponForgeCatalog.CritCap);
		p.SpeedMult = summon ? 1f : Math.Min(1f + speed, WeaponForgeCatalog.SpeedCap);
		p.KnockbackMult = summon ? 1f : Math.Min(1f + knock, WeaponForgeCatalog.KnockbackCap);
		p.VelocityMult = weapon.shoot > ProjectileID.None && weapon.shootSpeed > 0f ? Math.Min(1f + velocity, WeaponForgeCatalog.VelocityCap) : 1f;
		p.ScaleMult = p.Group == WeaponForgeGroup.Melee ? Math.Min(1f + scale, WeaponForgeCatalog.ScaleCap) : 1f;
	}

	private static bool Has(string value, params string[] words) => words.Any(w => value.Contains(w, StringComparison.Ordinal));
}
