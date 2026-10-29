using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.WeaponForge;

/// <summary>
/// v0.3 (owner, 29.10): every weapon of every loaded mod gets its own "Ding Dong forging",
/// in the spirit of the Night's Edge rework. The profile of a weapon is derived once from loaded
/// content only (item stats, recipes, crafting stations, materials, drop tables), which
/// tModLoader keeps identical on the server and every client, so nothing is synced or saved:
///  * a weapon that lags behind the weapons of its own class and rarity is raised towards them;
///  * the locked Eternity + Death difficulty adds a bonus that grows with the progression tier;
///  * the effort to obtain it (recipe depth, boss or rare drop) adds a little more;
///  * the crafting station, the dominant material and the weapon's home biome give it 1-3
///    individual traits (crit, speed, knockback, projectile speed, reach, biome attunement).
/// Everything is a bonus (never a nerf) applied through runtime stat hooks, so items, prefixes,
/// saves and other mods' own rebalances stay untouched, and every bonus has a hard cap.
/// </summary>
public sealed class WeaponForgeCatalog : ModSystem
{
	internal const float DamageCap = 1.6f, PeerFloorRatio = 0.85f, PeerFloorCap = 1.25f;
	internal const float DifficultyBase = 0.06f, DifficultyTierSpan = 0.12f;
	internal const float EffortPerDepth = 0.015f, EffortDepthCap = 0.06f, BossDropBonus = 0.05f, RareDropBonus = 0.04f;
	internal const float RareDropRate = 0.02f, AttunementBonus = 0.08f;
	internal const int CritCap = 8;
	internal const float SpeedCap = 1.12f, KnockbackCap = 1.25f, VelocityCap = 1.2f, ScaleCap = 1.12f;
	internal const int MinPeers = 4, MaxTraits = 3;

	private static WeaponForgeProfile[] profiles = Array.Empty<WeaponForgeProfile>();
	private static Dictionary<int, int> starterAmmo = new();
	internal static int WeaponCount { get; private set; }
	internal static int RaisedCount { get; private set; }

	internal static WeaponForgeProfile Get(int type) => type > 0 && type < profiles.Length ? profiles[type] : null;

	public override void PostSetupRecipes() => Build(Mod);

	public override void Unload()
	{
		profiles = Array.Empty<WeaponForgeProfile>();
		starterAmmo = new();
		WeaponForgeBossLadder.Unload();
		WeaponCount = RaisedCount = 0;
	}

	internal static void Build(Mod mod)
	{
		int count = ItemLoader.ItemCount;
		var result = new WeaponForgeProfile[count];
		var weapons = new List<Item>();
		for (int type = 1; type < count; type++)
			if (IsForgeable(type, out Item sample))
				weapons.Add(sample);

		starterAmmo = StarterAmmo();
		WeaponForgeBossLadder.Build(mod);
		var availability = new Dictionary<int, float>();
		Dictionary<int, Recipe> recipes = FirstRecipes();
		(HashSet<int> boss, Dictionary<int, float> bestRate) = DropSources();
		var depth = new Dictionary<int, int>();

		// Tier = rank of the rarity bucket by median damage (modded rarities slot in by power).
		var buckets = weapons.GroupBy(w => w.rare).Select(g => (Rare: g.Key, Median: Median(g.Select(w => (float)w.damage))))
			.OrderBy(b => b.Median).ThenBy(b => b.Rare).ToList();
		var tierOf = new Dictionary<int, float>();
		for (int i = 0; i < buckets.Count; i++)
			tierOf[buckets[i].Rare] = buckets.Count > 1 ? i / (float)(buckets.Count - 1) : 0f;

		var peerMedian = weapons.GroupBy(w => (Group(w), w.rare)).Where(g => g.Count() >= MinPeers)
			.ToDictionary(g => g.Key, g => Median(g.Select(Dps)));

		if (WeaponForgeBossLadder.Available) WeaponForgeBossLadder.CalibrateRarity(weapons, recipes, availability);
		int raised = 0;
		foreach (Item weapon in weapons)
		{
			var p = new WeaponForgeProfile { Group = Group(weapon), Tier = tierOf[weapon.rare] };
			float floor = 1f;
			if (peerMedian.TryGetValue((p.Group, weapon.rare), out float median) && Dps(weapon) > 0f && Dps(weapon) < median * PeerFloorRatio)
				floor = Math.Min(median * PeerFloorRatio / Dps(weapon), PeerFloorCap);
			p.PeerFloor = floor;
			recipes.TryGetValue(weapon.type, out Recipe recipe);
			if (WeaponForgeBossLadder.Available)
			{
				// Calibrated on the bosses this weapon is meant for, not on ordinary creatures.
				p.Availability = WeaponForgeBossLadder.AvailabilityOf(weapon, recipes, availability);
				(float ratio, WeaponForgeBossLadder.Rung[] targets) = WeaponForgeBossLadder.TargetsAfter(p.Availability);
				p.TargetRatio = ratio;
				p.TargetBoss = targets.Length > 0 ? targets[0].Name : "";
				p.DifficultyBonus = WeaponForgeBossLadder.BonusFor(ratio);
			}
			else
				p.DifficultyBonus = DifficultyBase + DifficultyTierSpan * p.Tier;

			int d = RecipeDepth(weapon.type, recipes, depth, 0);
			p.RecipeDepth = d;
			float effort = Math.Min(Math.Max(0, d - 1) * EffortPerDepth, EffortDepthCap);
			if (boss.Contains(weapon.type)) { effort += BossDropBonus; p.Traits.Add(WeaponForgeTrait.BossTrophy); }
			if (bestRate.TryGetValue(weapon.type, out float rate) && rate < RareDropRate) { effort += RareDropBonus; p.Traits.Add(WeaponForgeTrait.RareFind); }
			p.EffortBonus = effort;
			p.DamageMult = Math.Min(floor * (1f + p.DifficultyBonus + effort), DamageCap);

			if (recipe != null)
			{
				AddStationTrait(p, recipe);
				AddMaterialTrait(p, recipe);
			}
			p.Element = WeaponForgeTraits.ResolveElement(weapon, recipe);
			p.Attunement = WeaponForgeTraits.BiomeOf(p.Element);
			if (p.Traits.Count > MaxTraits) p.Traits.RemoveRange(MaxTraits, p.Traits.Count - MaxTraits);
			WeaponForgeTraits.Apply(p, weapon);
			if (floor > 1f) raised++;
			result[weapon.type] = p;
		}
		profiles = result;
		WeaponCount = weapons.Count;
		RaisedCount = raised;
		mod?.Logger.Info($"DINGDONG_WEAPON_FORGE|weapons={weapons.Count}|raisedToPeers={raised}|recipes={recipes.Count}|bossDrops={boss.Count}|rareDrops={bestRate.Count(r => r.Value < RareDropRate)}|tiers={buckets.Count}|ladder={WeaponForgeBossLadder.Available}|maxDamage={(weapons.Count > 0 ? weapons.Max(w => result[w.type].DamageMult) : 0f):0.00}");
	}

	// A real weapon whose content sample is itself (a few placeholder items reset to air).
	internal static bool IsForgeable(int type, out Item sample) =>
		CompositeWeaponLineage.IsWeapon(type) && ContentSamples.ItemsByType.TryGetValue(type, out sample) && sample.type == type
			|| (sample = null) != null;

	internal static WeaponForgeGroup Group(Item item)
	{
		if (item.CountsAsClass(DamageClass.Summon) || item.CountsAsClass(DamageClass.SummonMeleeSpeed)) return WeaponForgeGroup.Summon;
		if (item.CountsAsClass(DamageClass.Melee)) return WeaponForgeGroup.Melee;
		if (item.CountsAsClass(DamageClass.Ranged)) return WeaponForgeGroup.Ranged;
		if (item.CountsAsClass(DamageClass.Magic)) return WeaponForgeGroup.Magic;
		if (item.CountsAsClass(DamageClass.Throwing)) return WeaponForgeGroup.Throwing;
		return WeaponForgeGroup.Other;
	}

	// Damage per second at base stats; minions are compared on damage alone.
	internal static float Dps(Item item)
	{
		if (Group(item) == WeaponForgeGroup.Summon) return item.damage;
		int time = Group(item) == WeaponForgeGroup.Melee && item.shoot <= ProjectileID.None ? item.useAnimation : item.useTime;
		int ammo = item.useAmmo > AmmoID.None && starterAmmo.TryGetValue(item.useAmmo, out int a) ? a : 0;
		return (item.damage + ammo) * 60f / Math.Max(1, time);
	}

	// Damage of the cheapest ammunition of every ammo kind: guns and bows are compared with it,
	// since their own base damage leaves the bullet or arrow out.
	private static Dictionary<int, int> StarterAmmo()
	{
		var map = new Dictionary<int, int>();
		foreach (var group in ContentSamples.ItemsByType.Values.Where(i => i.ammo > AmmoID.None && i.damage > 0 && i.type > ItemID.None).GroupBy(i => i.ammo))
		{
			var starters = group.Where(i => i.rare <= ItemRarityID.Blue).Select(i => i.damage).ToList();
			map[group.Key] = starters.Count > 0 ? starters.Min() : group.Min(i => i.damage);
		}
		return map;
	}

	private static float Median(IEnumerable<float> values)
	{
		float[] sorted = values.OrderBy(v => v).ToArray();
		if (sorted.Length == 0) return 0f;
		int mid = sorted.Length / 2;
		return sorted.Length % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2f;
	}

	private static Dictionary<int, Recipe> FirstRecipes()
	{
		var map = new Dictionary<int, Recipe>();
		for (int i = 0; i < Recipe.numRecipes; i++)
		{
			Recipe recipe = Main.recipe[i];
			if (recipe?.createItem == null || recipe.createItem.IsAir || recipe.Disabled) continue;
			map.TryAdd(recipe.createItem.type, recipe);
		}
		return map;
	}

	// Longest chain of crafted ingredients below an item (1 = made from raw materials).
	private static int RecipeDepth(int type, Dictionary<int, Recipe> recipes, Dictionary<int, int> memo, int guard)
	{
		if (memo.TryGetValue(type, out int known)) return known;
		if (!recipes.TryGetValue(type, out Recipe recipe) || guard > 8) return 0;
		memo[type] = 1; // cycle guard (uncraft-style loops)
		int best = 0;
		foreach (Item ingredient in recipe.requiredItem)
			if (!ingredient.IsAir && ingredient.type != type)
				best = Math.Max(best, RecipeDepth(ingredient.type, recipes, memo, guard + 1));
		return memo[type] = Math.Min(best + 1, 8);
	}

	// Weapons that a boss (or a boss bag) drops, and the best drop rate of every dropped weapon.
	private static (HashSet<int>, Dictionary<int, float>) DropSources()
	{
		var boss = new HashSet<int>();
		var best = new Dictionary<int, float>();
		void Report(List<IItemDropRule> rules, bool fromBoss)
		{
			var rates = new List<DropRateInfo>();
			foreach (IItemDropRule rule in rules)
			{
				try { rule.ReportDroprates(rates, new DropRateInfoChainFeed(1f)); }
				catch (Exception) { /* a foreign rule that cannot report offline adds nothing */ }
			}
			foreach (DropRateInfo info in rates)
			{
				if (!CompositeWeaponLineage.IsWeapon(info.itemId)) continue;
				if (fromBoss) boss.Add(info.itemId);
				best[info.itemId] = best.TryGetValue(info.itemId, out float r) ? Math.Max(r, info.dropRate) : info.dropRate;
			}
		}
		for (int npc = NPCID.NegativeIDCount + 1; npc < NPCLoader.NPCCount; npc++)
		{
			if (npc == 0 || !ContentSamples.NpcsByNetId.TryGetValue(npc, out NPC sample)) continue;
			Report(Main.ItemDropsDB.GetRulesForNPCID(npc, includeGlobalDrops: false), sample.boss);
		}
		for (int bag = 1; bag < ItemLoader.ItemCount; bag++)
			if (ItemID.Sets.BossBag[bag])
				Report(Main.ItemDropsDB.GetRulesForItemID(bag), fromBoss: true);
		return (boss, best);
	}

	private static void AddStationTrait(WeaponForgeProfile p, Recipe recipe)
	{
		foreach (int tile in recipe.requiredTile)
		{
			WeaponForgeTrait? trait = WeaponForgeTraits.StationTrait(tile);
			if (trait is WeaponForgeTrait t) { p.Traits.Add(t); return; }
		}
	}

	private static void AddMaterialTrait(WeaponForgeProfile p, Recipe recipe)
	{
		Item dominant = recipe.requiredItem.Where(i => !i.IsAir && !CompositeWeaponLineage.IsWeapon(i.type))
			.OrderByDescending(i => i.stack).ThenBy(i => i.type).FirstOrDefault();
		if (dominant != null && WeaponForgeTraits.MaterialTrait(dominant) is WeaponForgeTrait t && !p.Traits.Contains(t))
			p.Traits.Add(t);
	}
}

internal enum WeaponForgeGroup : byte { Melee, Ranged, Magic, Summon, Throwing, Other }

internal sealed class WeaponForgeProfile
{
	public WeaponForgeGroup Group;
	public float Tier, PeerFloor = 1f, DifficultyBonus, EffortBonus, DamageMult = 1f;
	public int RecipeDepth, CritBonus;
	public float SpeedMult = 1f, KnockbackMult = 1f, VelocityMult = 1f, ScaleMult = 1f;
	public float Availability, TargetRatio = 1f;
	public string TargetBoss = "";
	public string Element = DamageTags.Neutral;
	public WeaponForgeBiome Attunement;
	public readonly List<WeaponForgeTrait> Traits = new();
}
