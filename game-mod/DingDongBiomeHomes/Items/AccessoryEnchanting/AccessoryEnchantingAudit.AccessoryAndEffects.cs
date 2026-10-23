using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes.Items.AccessoryEnchanting;
internal static partial class AccessoryEnchantingAudit
{
	private static void RunAccessory(List<string> failures, ref int fusions, ref int rejections, ref int persistence, Player player, Item target, AccessoryEnchantGlobalItem targetState, AccessoryEnchantmentId[] selected, out TagCompound tag, out Item loaded)
	{
		foreach (AccessoryEnchantmentId enchantment in selected)
		{
			Item donor = Accessory(ItemID.Aglet);
			donor.GetGlobalItem<AccessoryEnchantGlobalItem>().SetImprintForProbe(enchantment);
			if (AccessoryEnchantingService.TryFuse(player, target, donor, charge: false, out string failure) && donor.IsAir && targetState.Fused.Contains(enchantment))
				fusions++;
			else
				Fail(failures, $"fusion:{enchantment}:{failure}");
		}

		if (!AccessoryEnchantingService.TryUpgrade(player, target, charge: false, out string maxFailure) && maxFailure == "maximum-level" && targetState.Level == AccessoryEnchantGlobalItem.MaxLevel)
			rejections++;
		else
			Fail(failures, $"max-rejection:{maxFailure}");
		Item fullDonor = Accessory(ItemID.AnkletoftheWind);
		fullDonor.GetGlobalItem<AccessoryEnchantGlobalItem>().SetImprintForProbe(AccessoryEnchantmentId.Menace);
		if (!AccessoryEnchantingService.TryFuse(player, target, fullDonor, charge: false, out string fullFailure) && fullFailure == "no-unlocked-slot" && !fullDonor.IsAir)
			rejections++;
		else
			Fail(failures, $"full-rejection:{fullFailure}");
		Item weapon = new();
		weapon.SetDefaults(ItemID.WoodenSword);
		if (!AccessoryEnchantingService.TryUpgrade(player, weapon, charge: false, out string accessoryFailure) && accessoryFailure == "accessory-only" && weapon.type == ItemID.WoodenSword)
			rejections++;
		else
			Fail(failures, $"accessory-rejection:{accessoryFailure}");
		Item clone = target.Clone();
		AccessoryEnchantGlobalItem cloneState = clone.GetGlobalItem<AccessoryEnchantGlobalItem>();
		if (Equivalent(targetState, cloneState))
			persistence++;
		else
			Fail(failures, "clone");
		tag = new();
		targetState.SaveData(target, tag);
		loaded = Accessory(ItemID.HermesBoots);
	}

	private static void RunEffects(Player player, IReadOnlyList<AccessoryEnchantmentId> selected, ref int effects, List<string> failures)
	{
		AccessoryEnchantPlayer modPlayer = player.GetModPlayer<AccessoryEnchantPlayer>();
		modPlayer.ResetEffects();
		foreach (AccessoryEnchantmentId value in selected)
			modPlayer.Apply(value);
		int defenseBefore = player.statDefense;
		int lifeBefore = player.statLifeMax2;
		float luckBefore = player.luck;
		modPlayer.PostUpdateEquips();
		if (player.statDefense >= defenseBefore + 7)
			effects++;
		else
			Fail(failures, "effect:bastion-vitality-defense");
		if (player.statLifeMax2 >= lifeBefore + 40)
			effects++;
		else
			Fail(failures, "effect:bastion-vitality-life");
		if (player.luck >= luckBefore + 0.119f)
			effects++;
		else
			Fail(failures, "effect:fortune");
		if (ModContent.ProjectileType<AccessoryEchoProjectile>() > ProjectileID.None)
			effects++;
		else
			Fail(failures, "effect:echo-projectile");
	}
}
