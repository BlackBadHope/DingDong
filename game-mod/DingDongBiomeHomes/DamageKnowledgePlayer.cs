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
public sealed class DamageKnowledgePlayer : ModPlayer
{
	private const string SaveKey = "damageBannerKnowledge";
	private readonly HashSet<int> learnedBannerItems = new();
	public override void Initialize() => learnedBannerItems.Clear();
	public override void PostUpdate()
	{
		if (Main.netMode == NetmodeID.Server || Player.whoAmI != Main.myPlayer || Main.GameUpdateCount % 30 != 0)
			return;
		foreach (Item item in Player.inventory)
		{
			if (!item.IsAir && DamageKnowledgeSystem.IsKnownBannerItem(item.type))
				learnedBannerItems.Add(item.type);
		}
	}

	public bool HasLearned(int bannerItemType) => bannerItemType <= ItemID.None || learnedBannerItems.Contains(bannerItemType);
	public override void SaveData(TagCompound tag)
	{
		if (learnedBannerItems.Count > 0)
			tag[SaveKey] = learnedBannerItems.OrderBy(value => value).ToList();
	}

	public override void LoadData(TagCompound tag)
	{
		learnedBannerItems.Clear();
		if (tag.ContainsKey(SaveKey))
		{
			foreach (int itemType in tag.GetList<int>(SaveKey))
			{
				if (itemType > ItemID.None)
					learnedBannerItems.Add(itemType);
			}
		}
	}
}
