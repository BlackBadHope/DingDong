using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.VanillaRebalance;

/// <summary>
/// Night's Edge is the pre-Wall-of-Flesh crafting capstone, but on the ×10
/// continent with For The Worthy scaling it could not even carry Skeletron.
/// This floor-raise keeps it the "four blades" reward: higher base damage,
/// a wider arc and armor penetration that matters against FTW-scaled defense.
/// Math.Max floors never nerf another mod's own rebalance of the same sword,
/// and the shadow element ties it into the elemental on-hit system
/// (ShadowFlame damage-over-time), so the buff is felt, not cosmetic.
/// </summary>
public sealed class NightsEdgeProgression : GlobalItem
{
	internal const int DamageFloor = 72;
	internal const int ArmorPenetrationFloor = 12;
	// Owner request: the swing must cover ~50% more area around the player.
	// The 1.4.4 swing arc scales linearly with item scale, so 1.5 widens the
	// covered arc by half in each direction.
	internal const float ScaleFloor = 1.5f;

	public override bool AppliesToEntity(Item entity, bool lateInstantiation) => entity.type == ItemID.NightsEdge;

	public override void SetDefaults(Item item)
	{
		item.damage = Math.Max(item.damage, DamageFloor);
		item.ArmorPenetration = Math.Max(item.ArmorPenetration, ArmorPenetrationFloor);
		item.scale = Math.Max(item.scale, ScaleFloor);
	}

	public override void UseItemHitbox(Item item, Player player, ref Microsoft.Xna.Framework.Rectangle hitbox, ref bool noHitbox)
	{
		// Classic (non-projectile) swings get the same wider coverage.
		hitbox.Inflate(hitbox.Width / 4, hitbox.Height / 4);
	}

	public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
	{
		tooltips.Add(new TooltipLine(Mod, "DingDongNightsEdge",
			Language.GetTextValue("Mods.DingDongBiomeHomes.VanillaRebalance.NightsEdge")));
	}
}
