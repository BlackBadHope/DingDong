using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;
/// <summary>
/// Persistent per-item origin and element metadata. These values travel with
/// the concrete item stack instead of being recomputed from the current owner.
/// </summary>
public sealed partial class DamageAffinityGlobalItem
{
	public override void OnCreated(Item item, ItemCreationContext context)
	{
		if (item.type == ItemID.SlimeStaff || item.type == ItemID.BabyBirdStaff)
			return;
		AcquisitionTag = context switch
		{
			RecipeItemCreationContext => DamageTags.Crafted,
			BuyItemCreationContext => DamageTags.Shop,
			_ => AcquisitionTag
		};
	}

	internal static void MarkRecipePrototype(Item item)
	{
		if (item is null || item.IsAir || item.type == ItemID.SlimeStaff || item.type == ItemID.BabyBirdStaff)
			return;

		item.GetGlobalItem<DamageAffinityGlobalItem>().AcquisitionTag = DamageTags.Crafted;
	}
}
