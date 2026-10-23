using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.ID;

namespace DingDongBiomeHomes;
internal static partial class ResidentQuestCatalog
{
	private struct StableRandom
	{
		private uint state;
		internal StableRandom(uint seed) => state = seed;
		internal int Next(int exclusiveMaximum)
		{
			state ^= state << 13;
			state ^= state >> 17;
			state ^= state << 5;
			return (int)(state % (uint)exclusiveMaximum);
		}
	}
}
