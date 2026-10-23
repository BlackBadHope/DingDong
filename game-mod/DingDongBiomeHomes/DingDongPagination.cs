using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
internal static class DingDongPagination
{
	internal static int Move(int currentIndex, int delta, int pageCount)
	{
		if (pageCount <= 0)
			return 0;
		int normalized = ((currentIndex % pageCount) + pageCount) % pageCount;
		return ((normalized + delta) % pageCount + pageCount) % pageCount;
	}

	internal static bool ContractPass(out string failure)
	{
		failure = string.Empty;
		if (Move(0, 1, 8) != 1)
			failure = "next";
		else if (Move(7, 1, 8) != 0)
			failure = "next-wrap";
		else if (Move(0, -1, 8) != 7)
			failure = "previous-wrap";
		else if (Move(4, -1, 8) != 3)
			failure = "previous";
		else if (Move(-1, 0, 8) != 7)
			failure = "normalize-negative";
		else if (Move(99, 0, 8) != 3)
			failure = "normalize-large";
		else if (Move(0, 1, 0) != 0)
			failure = "empty";
		return failure.Length == 0;
	}
}
