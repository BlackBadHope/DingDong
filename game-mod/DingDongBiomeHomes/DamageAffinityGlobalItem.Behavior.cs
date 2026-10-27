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
	public override GlobalItem Clone(Item from, Item to)
	{
		DamageAffinityGlobalItem clone = (DamageAffinityGlobalItem)base.Clone(from, to);
		clone.AcquisitionTag = AcquisitionTag;
		clone.ElementTag = ElementTag;
		return clone;
	}

	public override bool CanStack(Item destination, Item source)
	{
		if (!destination.TryGetGlobalItem(out DamageAffinityGlobalItem destinationAffinity) || !source.TryGetGlobalItem(out DamageAffinityGlobalItem sourceAffinity))
		{
			return true;
		}

		// Native stackability decides whether origin is fungible. Combat damage,
		// a projectile, or an equipment slot can also belong to ordinary stacks
		// (grenades, buckets, monoliths). Prefixes and enchantments have their own
		// native/GlobalItem vetoes and must not be replaced with origin checks.
		// Crafting also compares its result prototype with the item on the cursor
		// before every repeat. Different origins must not block normal stacking.
		bool stackablePair = destination.type == source.type && CanMergeOrigins(destination) && CanMergeOrigins(source);
		return (stackablePair || string.Equals(destinationAffinity.AcquisitionTag, sourceAffinity.AcquisitionTag, StringComparison.Ordinal))
			&& string.Equals(destinationAffinity.ElementTag, sourceAffinity.ElementTag, StringComparison.Ordinal);
	}

	private static bool CanMergeOrigins(Item item)
	{
		return item.maxStack > 1;
	}

	public override void SetDefaults(Item entity)
	{
		(AcquisitionTag, ElementTag) = DefaultsFor(entity);
	}

	public override void UseAnimation(Item item, Player player)
	{
		if ((item.damage > 0 || item.shoot > ProjectileID.None) && Main.netMode != NetmodeID.Server && player.whoAmI == Main.myPlayer)
		{
			player.GetModPlayer<DamageCombatPlayer>().BeginAttack();
		}
	}

	internal static (string Acquisition, string Element) DefaultsFor(Item item)
	{
		if (item.type == ItemID.SlimeStaff)
			return (DamageTags.CreatureDrop, DamageTags.Gel);
		// The four-blade capstone is explicitly shadow so its rebalance rides the
		// elemental on-hit system instead of relying on lexical inference.
		if (item.type == ItemID.NightsEdge)
			return (DamageTags.Crafted, DamageTags.Shadow);
		if (item.type == ItemID.BabyBirdStaff)
			return (DamageTags.Worldgen, DamageTags.Nature);
		return (DamageTags.UnspecifiedAcquisition, DamageRuntimeClassifier.InferElement(DamageRuntimeClassifier.StableItemIdentity(item)));
	}
}
