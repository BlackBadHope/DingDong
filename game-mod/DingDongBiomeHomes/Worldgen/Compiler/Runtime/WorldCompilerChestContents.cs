using Terraria;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static class WorldCompilerChestContents
{
	internal static int FindPrimary(Chest chest, int expected) =>
		WorldCompilerChestSlotPolicy.Find(expected, chest?.item?.Length ?? 0, slot => {
			var item = chest.item[slot];
			return (item?.type ?? 0, item?.stack ?? 0);
		});
}
