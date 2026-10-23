using System;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static class WorldCompilerChestSlotPolicy
{
	// Mods may insert loot ahead of the original primary item. Its slot is not a contract.
	internal static int Find(int expected, int count, Func<int, (int Type, int Stack)> read)
	{
		if (expected <= 0) return -1;
		for (int slot = 0; slot < count; slot++)
		{
			var item = read(slot);
			if (item.Type == expected && item.Stack > 0) return slot;
		}
		return -1;
	}
}
