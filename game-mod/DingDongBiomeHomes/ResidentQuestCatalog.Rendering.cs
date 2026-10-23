using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.ID;

namespace DingDongBiomeHomes;
internal static partial class ResidentQuestCatalog
{
	private static int[] BuildRawOrder(string persistentId, string npcKey, int cycle)
	{
		int[] order = Enumerable.Range(0, PoolSize).ToArray();
		StableRandom random = new(StableHash($"quest-3.0|{persistentId}|{npcKey}|{cycle}"));
		for (int index = order.Length - 1; index > 0; index--)
		{
			int other = random.Next(index + 1);
			(order[index], order[other]) = (order[other], order[index]);
		}

		return order;
	}
}
