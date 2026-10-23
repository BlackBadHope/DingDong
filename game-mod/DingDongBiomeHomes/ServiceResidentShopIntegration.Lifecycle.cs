using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;

namespace DingDongBiomeHomes;
internal static partial class ServiceResidentShopIntegration
{
	private static void InitializeShops(OrigInitializeShops orig)
	{
		orig();
		ServiceResidentsByType.Clear();
		HashSet<int> npcTypesWithShops = NPCShopDatabase.AllShops.Select(shop => shop.NpcType).ToHashSet();
		int resolvedResidents = 0;
		int standardResidents = 0;
		int missingResidents = 0;
		foreach (NpcTownIdentity identity in NpcTownManifest.All.OrderBy(entry => entry.NpcKey, StringComparer.Ordinal))
		{
			if (!TryResolveNpcType(identity.NpcKey, out int npcType))
			{
				missingResidents++;
				continue;
			}

			resolvedResidents++;
			if (npcTypesWithShops.Contains(npcType))
			{
				standardResidents++;
				continue;
			}

			int itemType = NpcShopItemResolver.Resolve(identity.ShopItemName, identity.Variant);
			if (itemType <= ItemID.None)
			{
				missingResidents++;
				continue;
			}

			Item item = new(itemType)
			{
				shopCustomPrice = Item.buyPrice(silver: identity.ShopPriceSilver)
			};
			new NPCShop(npcType, ShopName).Add(item).Register();
			ServiceResidentsByType.Add(npcType, identity);
			ModContent.GetInstance<DingDongBiomeHomes>().Logger.Info($"Service resident shop registered: npc={identity.NpcKey}; shop={NPCShopDatabase.GetShopName(npcType, ShopName)}; item={ItemID.Search.GetName(itemType)}; priceSilver={identity.ShopPriceSilver}");
		}

		ModContent.GetInstance<DingDongBiomeHomes>().Logger.Info($"Resident shop coverage: manifest={NpcTownManifest.AuthoredCount}, resolved={resolvedResidents}, standard={standardResidents}, service={ServiceResidentsByType.Count}, missing={missingResidents}");
	}
}
