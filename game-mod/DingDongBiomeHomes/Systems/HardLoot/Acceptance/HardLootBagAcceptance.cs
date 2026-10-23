using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Systems.HardLoot.Acceptance;

internal static class HardLootBagAcceptance
{
	private static readonly Dictionary<int, int> Required = new()
	{
		[ItemID.KingSlimeBossBag] = ItemID.RoyalGel,
		[ItemID.EyeOfCthulhuBossBag] = ItemID.EoCShield,
		[ItemID.BrainOfCthulhuBossBag] = ItemID.BrainOfConfusion,
		[ItemID.QueenBeeBossBag] = ItemID.HiveBackpack,
		[ItemID.SkeletronBossBag] = ItemID.BoneGlove
	};

	internal static (int Bags, int Checks) Run(Player player)
	{
		int bags = 0, checks = 0;
		List<string> failures = [];
		HashSet<int> seenRequired = [];
		HashSet<int> types = HardLootBagBaseline.Types.Concat(Required.Keys).ToHashSet();
		for (int type = 1; type < ItemLoader.ItemCount; type++)
			if (ItemID.Sets.BossBag[type] && !ItemID.Sets.Deprecated[type]) types.Add(type);
		foreach (int type in types.OrderBy(type => type))
		{
			Dictionary<int, int> guaranteed;
			try
			{
				if (!ItemID.Sets.BossBag[type] || ItemID.Sets.Deprecated[type])
					throw new InvalidOperationException("required/captured boss bag disappeared");
				List<IItemDropRule> rules = Main.ItemDropsDB.GetRulesForItemID(type);
				var info = new DropAttemptInfo { item = type, player = player,
					IsExpertMode = true, IsMasterMode = true, rng = Main.rand };
				guaranteed = HardLootBagBaseline.Original(type, info);
				if (Required.TryGetValue(type, out int known))
					guaranteed[known] = Math.Max(1, guaranteed.GetValueOrDefault(known));
				Dictionary<int, int> current = HardLootBagBaseline.Current(rules, info);
				foreach ((int item, int minimum) in guaranteed)
					if (current.GetValueOrDefault(item) < minimum)
						failures.Add($"Bag {type}: normalized registry lost guarantee {item}x{minimum}");
				if (Required.ContainsKey(type)) seenRequired.Add(type);
			}
			catch (Exception error)
			{
				failures.Add($"Bag {type} setup: {error}");
				continue;
			}
			for (int repeat = 0; repeat < 3; repeat++)
			{
				try
				{
					Dictionary<int, int> actual = HardLootAcceptanceCapture.Observe(
						() => player.OpenBossBag(type));
					if (actual.Count == 0) failures.Add($"Bag {type}: no items");
					foreach ((int item, int minimum) in guaranteed)
					{
						if (!actual.TryGetValue(item, out int count) || count < minimum)
							failures.Add($"Bag {type}: missing guaranteed item {item}");
						else checks++;
					}
				}
				catch (Exception error) { failures.Add($"Bag {type}: {error}"); }
			}
			bags++;
		}
		foreach (int type in Required.Keys.Except(seenRequired))
			failures.Add($"Required vanilla boss bag {type} was not checked");
		if (bags < 20 || checks < Required.Count * 3)
			failures.Add($"Insufficient boss bag coverage: {bags}/{checks}");
		if (failures.Count > 0)
			throw new InvalidOperationException(string.Join("; ", failures.Distinct()));
		return (bags, checks);
	}
}
