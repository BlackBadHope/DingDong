using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using DingDongBiomeHomes.Compatibility;

namespace DingDongBiomeHomes.Items.WeaponForge;

/// <summary>
/// The boss ladder behind weapon forging (owner, 29.10: "Zenith is made for the stage where normal
/// mobs no longer matter — calibrate against the bosses"). Every boss and miniboss of every loaded
/// mod comes from Boss Checklist in progression order; its main body is measured once at load time
/// at normal difficulty and at the locked Ding Dong difficulty (Master + For the Worthy + Eternity/
/// Masochist + Revengeance/Death). A weapon becomes available after the earliest boss that drops it
/// or anything in its recipe tree; it is meant for the next bosses, and how much harder those became
/// (median life ratio of the next four) sets its difficulty bonus. Measured on every peer from loaded
/// content only, so server and clients agree without any sync.
/// </summary>
internal static class WeaponForgeBossLadder
{
	internal const int TargetCount = 4;
	internal const float Exponent = 0.2f, MinBonus = 0.04f, MaxBonus = 0.45f;

	internal sealed record Rung(string Key, string Name, float Progression, bool IsBoss, int MainNpc, int BaseLife, int LockedLife, int[] Loot)
	{
		public float Ratio => BaseLife > 0 && LockedLife > 0 ? LockedLife / (float)BaseLife : 1f;
	}

	internal static Rung[] Rungs { get; private set; } = Array.Empty<Rung>();
	private static Rung[] bosses = Array.Empty<Rung>();
	private static Dictionary<int, float> lootAvailability = new();
	private static HashSet<int> ordinaryDrops = new();
	private static Dictionary<int, List<Recipe>> allRecipes = new();
	private static Dictionary<int, float> rarityFloor = new();
	internal static bool Available => bosses.Length >= TargetCount;

	internal static void Unload()
	{
		Rungs = bosses = Array.Empty<Rung>();
		lootAvailability = new();
		ordinaryDrops = new();
		allRecipes = new();
		rarityFloor = new();
	}

	internal static void Build(Mod mod)
	{
		Unload();
		if (!ModLoader.TryGetMod("BossChecklist", out Mod checklist)) return;
		IDictionary info;
		try { info = checklist.Call("GetBossInfoDictionary", mod, "1.6") as IDictionary; }
		catch (Exception ex) { mod.Logger.Warn("DINGDONG_WEAPON_FORGE_LADDER|verdict=UNAVAILABLE|reason=" + ex.Message); return; }
		if (info == null) return;

		var raw = new List<(string Key, string Name, float Prog, bool Boss, int Npc, int[] Loot)>();
		foreach (DictionaryEntry entry in info)
		{
			if (entry.Value is not IDictionary d || Flag(d, "isEvent")) continue;
			bool boss = Flag(d, "isBoss"), mini = Flag(d, "isMiniboss");
			if (!boss && !mini) continue;
			int[] npcs = Ints(d, "npcIDs");
			if (npcs.Length == 0 || !d.Contains("progression")) continue;
			raw.Add((entry.Key.ToString(), d.Contains("displayName") ? d["displayName"]?.ToString() ?? "" : "", Convert.ToSingle(d["progression"]), boss, npcs[0], Ints(d, "loot")));
		}
		raw.Sort((a, b) => a.Prog != b.Prog ? a.Prog.CompareTo(b.Prog) : string.CompareOrdinal(a.Key, b.Key));
		int[] baseLife = DifficultyLockSystem.WithDifficulty(false, () => raw.Select(r => Life(r.Npc)).ToArray());
		int[] lockedLife = DifficultyLockSystem.WithDifficulty(true, () => raw.Select(r => Life(r.Npc)).ToArray());
		Rungs = raw.Select((r, i) => new Rung(r.Key, r.Name, r.Prog, r.Boss, r.Npc, baseLife[i], lockedLife[i], r.Loot)).ToArray();
		bosses = Rungs.Where(r => r.IsBoss && r.BaseLife > 0 && r.LockedLife > 0).ToArray();
		foreach (Rung rung in Rungs)
			foreach (int item in rung.Loot)
				if (!lootAvailability.TryGetValue(item, out float known) || rung.Progression < known)
					lootAvailability[item] = rung.Progression;
		// Anything an ordinary creature drops is not gated by a boss (shark fins, souls of light...).
		var bossNpcs = new HashSet<int>(raw.Select(r => r.Npc));
		for (int npc = 1; npc < NPCLoader.NPCCount; npc++)
		{
			if (bossNpcs.Contains(npc) || !ContentSamples.NpcsByNetId.TryGetValue(npc, out NPC sample) || sample.boss || NPCID.Sets.ShouldBeCountedAsBoss[npc]) continue;
			var rates = new List<Terraria.GameContent.ItemDropRules.DropRateInfo>();
			foreach (var rule in Main.ItemDropsDB.GetRulesForNPCID(npc, includeGlobalDrops: false))
			{
				try { rule.ReportDroprates(rates, new Terraria.GameContent.ItemDropRules.DropRateInfoChainFeed(1f)); }
				catch (Exception) { }
			}
			foreach (var r in rates) ordinaryDrops.Add(r.itemId);
		}
		for (int i = 0; i < Recipe.numRecipes; i++)
		{
			Recipe recipe = Main.recipe[i];
			if (recipe?.createItem == null || recipe.createItem.IsAir || recipe.Disabled) continue;
			if (!allRecipes.TryGetValue(recipe.createItem.type, out var list)) allRecipes[recipe.createItem.type] = list = new List<Recipe>();
			list.Add(recipe);
		}
		mod.Logger.Info($"DINGDONG_WEAPON_FORGE_LADDER|verdict={(Available ? "PASS" : "FALLBACK")}|rungs={Rungs.Length}|bosses={bosses.Length}|lootItems={lootAvailability.Count}|ordinaryDrops={ordinaryDrops.Count}" +
			$"|medianRatio={(bosses.Length > 0 ? bosses.Select(b => b.Ratio).OrderBy(x => x).ElementAt(bosses.Length / 2) : 0f):0.00}");
	}

	// Earliest progression at which an item can be had: dropped by a boss, or crafted from things
	// that can all be had (the latest of its ingredients); never earlier than its rarity implies.
	internal static float AvailabilityOf(Item item, Dictionary<int, Recipe> recipes, Dictionary<int, float> memo) =>
		Math.Max(Linked(item.type, recipes, memo, 0), RarityFloor(item.rare));

	/// <summary>Learns, per rarity, where the boss-gated weapons of that rarity sit on the ladder
	/// (lower quartile), so a weapon of any mod that no boss or recipe links still lands at its stage.</summary>
	internal static void CalibrateRarity(IEnumerable<Item> weapons, Dictionary<int, Recipe> recipes, Dictionary<int, float> memo)
	{
		rarityFloor = new();
		foreach (var group in weapons.Where(w => w.rare >= ItemRarityID.Green).GroupBy(w => w.rare))
		{
			float[] gated = group.Select(w => Linked(w.type, recipes, memo, 0)).Where(v => v > 0f).OrderBy(v => v).ToArray();
			if (gated.Length >= 5) rarityFloor[group.Key] = gated[gated.Length / 4];
		}
	}

	private static float Linked(int type, Dictionary<int, Recipe> recipes, Dictionary<int, float> memo, int depth)
	{
		if (memo.TryGetValue(type, out float known)) return known;
		if (ordinaryDrops.Contains(type)) return memo[type] = 0f;
		float direct = lootAvailability.TryGetValue(type, out float drop) ? drop : float.MaxValue;
		float crafted = float.MaxValue;
		if (depth < 10 && allRecipes.TryGetValue(type, out List<Recipe> options))
		{
			memo[type] = direct == float.MaxValue ? 0f : direct; // cycle guard
			// Any recipe will do: the earliest one counts; within it, the latest ingredient gates it.
			foreach (Recipe recipe in options)
			{
				float gate = 0f;
				foreach (Item ingredient in recipe.requiredItem)
					if (!ingredient.IsAir && ingredient.type != type)
						gate = Math.Max(gate, Linked(ingredient.type, recipes, memo, depth + 1));
				crafted = Math.Min(crafted, gate);
			}
		}
		float result = Math.Min(direct, crafted);
		if (result == float.MaxValue) result = 0f;
		return memo[type] = result;
	}

	// Safety floor from rarity: vanilla gates, and the Calamity rarities by name.
	internal static float RarityFloor(int rare)
	{
		if (rarityFloor.TryGetValue(rare, out float learned)) return learned;
		if (rare >= ItemRarityID.Count)
			return RarityLoader.GetRarity(rare)?.Name switch
			{
				"Turquoise" => 19f, "PureGreen" => 20f, "DarkBlue" => 21f, "Violet" => 22f,
				"HotPink" or "CalamityRed" => 23f, _ => 18f
			};
		return rare switch
		{
			<= 1 => 0f, 2 => 2f, 3 => 3f, 4 or 5 => 7f, 6 => 9f, 7 => 12f, 8 => 13f, 9 => 17f, _ => 18f
		};
	}

	/// <summary>The next bosses after <paramref name="available"/> and the median of how much more
	/// life they have at the locked difficulty; the end of the ladder falls back to the last bosses.</summary>
	internal static (float Ratio, Rung[] Targets) TargetsAfter(float available)
	{
		Rung[] next = bosses.Where(b => b.Progression > available).Take(TargetCount).ToArray();
		if (next.Length < TargetCount) next = bosses.Skip(Math.Max(0, bosses.Length - TargetCount)).ToArray();
		float[] ratios = next.Select(b => b.Ratio).OrderBy(r => r).ToArray();
		float median = ratios.Length == 0 ? 1f : ratios.Length % 2 == 1 ? ratios[ratios.Length / 2] : (ratios[ratios.Length / 2 - 1] + ratios[ratios.Length / 2]) / 2f;
		return (median, next);
	}

	internal static float BonusFor(float ratio) => Math.Clamp(MathF.Pow(Math.Max(1f, ratio), Exponent) - 1f, MinBonus, MaxBonus);

	private static int Life(int npc)
	{
		try { var n = new NPC(); n.SetDefaults(npc); return n.lifeMax; }
		catch (Exception) { return 0; }
	}

	private static bool Flag(IDictionary d, string key) => d.Contains(key) && d[key] is bool b && b;

	private static int[] Ints(IDictionary d, string key) =>
		d.Contains(key) && d[key] is IEnumerable values ? values.Cast<object>().Select(Convert.ToInt32).ToArray() : Array.Empty<int>();
}
