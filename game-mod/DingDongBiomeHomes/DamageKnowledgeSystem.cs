using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace DingDongBiomeHomes;
public sealed class DamageKnowledgeSystem : ModSystem
{
	private static readonly HashSet<int> BannerItems = new();
	public override void PostSetupContent()
	{
		BannerItems.Clear();
		foreach (NPC npc in ContentSamples.NpcsByNetId.Values)
		{
			int itemType = BannerItemFor(npc);
			if (itemType > ItemID.None)
				BannerItems.Add(itemType);
		}
	}

	public override void Unload() => BannerItems.Clear();
	internal static bool IsKnownBannerItem(int itemType) => BannerItems.Contains(itemType);
	internal static int BannerItemFor(NPC npc)
	{
		int bannerNpcType = npc.BannerID();
		int bannerId = Item.NPCtoBanner(bannerNpcType);
		return bannerId > 0 ? Item.BannerToItem(bannerId) : ItemID.None;
	}
}
