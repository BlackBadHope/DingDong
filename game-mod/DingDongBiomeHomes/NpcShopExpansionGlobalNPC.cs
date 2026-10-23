using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

public sealed class NpcShopExpansionGlobalNPC : GlobalNPC
{
	private static readonly HashSet<int> AugmentedNpcTypes = new();

	public override void ModifyShop(NPCShop shop)
	{
		if (!AugmentedNpcTypes.Add(shop.NpcType))
			return;

		if (!ContentSamples.NpcsByNetId.TryGetValue(shop.NpcType, out NPC sample))
			return;

		string npcKey = sample.ModNPC?.FullName ?? $"Terraria/{NPCID.Search.GetName(shop.NpcType)}";
		if (!NpcTownManifest.TryGet(npcKey, out NpcTownIdentity identity))
			return;

		int itemType = NpcShopItemResolver.Resolve(
			identity.ShopItemName,
			identity.Variant,
			candidate => shop.TryGetEntry(candidate, out _));

		if (itemType <= ItemID.None)
		{
			Mod.Logger.Warn($"No conflict-free shop item found for {npcKey}.");
			return;
		}

		Item item = new(itemType)
		{
			shopCustomPrice = Item.buyPrice(silver: identity.ShopPriceSilver)
		};
		shop.Add(item);
		Mod.Logger.Info($"Resident shop expanded: npc={npcKey}; shop={shop.FullName}; item={ItemID.Search.GetName(itemType)}; priceSilver={identity.ShopPriceSilver}");
	}

	public override void Unload()
	{
		AugmentedNpcTypes.Clear();
	}
}
