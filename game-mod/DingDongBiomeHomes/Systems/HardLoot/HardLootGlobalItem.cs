using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Systems.HardLoot;

public sealed class HardLootGlobalItem : GlobalItem
{
	public override void ModifyItemLoot(Item item, ItemLoot itemLoot)
	{
		Acceptance.HardLootBagBaseline.Capture(item, itemLoot);
		CalamityPityItemLootAdapter.Capture(item, itemLoot);
		ThoriumMultipleItemLootAdapter.Capture(item, itemLoot);
		AndroLibBagDifficultyAdapter.Capture(item, itemLoot);
	}
}
