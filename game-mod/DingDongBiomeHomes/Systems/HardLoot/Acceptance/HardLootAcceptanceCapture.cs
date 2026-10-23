using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Systems.HardLoot.Acceptance;

public sealed class HardLootAcceptanceCapture : GlobalItem
{
	private static List<(Item Item, int Type, int Stack)> spawned;
	public override void OnSpawn(Item item, IEntitySource source) =>
		spawned?.Add((item, item.type, item.stack));

	internal static Dictionary<int, int> Observe(Action action)
	{
		if (spawned != null) throw new InvalidOperationException("Nested loot capture.");
		spawned = [];
		try
		{
			action();
			Dictionary<int, int> result = [];
			foreach (var entry in spawned)
				if (entry.Type > 0 && entry.Stack > 0)
					result[entry.Type] = result.GetValueOrDefault(entry.Type) + entry.Stack;
			return result;
		}
		finally
		{
			// Only items emitted synchronously by this test are removed.
			foreach (var entry in spawned) entry.Item.TurnToAir();
			spawned = null;
		}
	}
}
