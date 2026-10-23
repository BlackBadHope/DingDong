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
	public override void OnSpawn(Item item, IEntitySource source)
	{
		if (item.type == ItemID.SlimeStaff || item.type == ItemID.BabyBirdStaff)
			return;
		AcquisitionTag = ResolveSpawnAcquisition(source, AcquisitionTag);
	}

	public override void PostUpdate(Item item)
	{
		// Shimmer transforms an existing world-item entity rather than creating a
		// dedicated ItemCreationContext. The synchronized shimmer flag is the
		// authoritative signal that the resulting concrete item was transmuted.
		if (item.shimmered)
			AcquisitionTag = DamageTags.Transmutation;
	}
}
