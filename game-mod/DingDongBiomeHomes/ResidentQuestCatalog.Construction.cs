using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.ID;

namespace DingDongBiomeHomes;
internal static partial class ResidentQuestCatalog
{
	private static int[] BuildOrder(string persistentId, string npcKey, int cycle)
	{
		int[] order = BuildRawOrder(persistentId, npcKey, cycle);
		if (cycle > 0)
		{
			int[] previous = BuildRawOrder(persistentId, npcKey, cycle - 1);
			if (order[0] == previous[^1])
				(order[0], order[1]) = (order[1], order[0]);
		}

		return order;
	}
}
