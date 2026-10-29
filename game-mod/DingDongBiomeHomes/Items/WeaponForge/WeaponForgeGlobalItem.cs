using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.WeaponForge;

/// <summary>Applies the forging profile of <see cref="WeaponForgeCatalog"/> at use time.</summary>
public sealed class WeaponForgeGlobalItem : GlobalItem
{
	private const string Key = "Mods.DingDongBiomeHomes.WeaponForge.";
	private static readonly Color LineColor = new(255, 190, 110);

	public override void ModifyWeaponDamage(Item item, Player player, ref StatModifier damage)
	{
		if (WeaponForgeCatalog.Get(item.type) is not WeaponForgeProfile p) return;
		damage *= p.DamageMult;
		if (p.Attunement != WeaponForgeBiome.None && WeaponForgeTraits.InBiome(player, p.Attunement))
			damage += WeaponForgeCatalog.AttunementBonus;
	}

	public override void ModifyWeaponCrit(Item item, Player player, ref float crit)
	{
		if (WeaponForgeCatalog.Get(item.type) is WeaponForgeProfile p) crit += p.CritBonus;
	}

	public override float UseSpeedMultiplier(Item item, Player player) =>
		WeaponForgeCatalog.Get(item.type) is WeaponForgeProfile p ? p.SpeedMult : 1f;

	public override void ModifyItemScale(Item item, Player player, ref float scale)
	{
		if (WeaponForgeCatalog.Get(item.type) is WeaponForgeProfile p) scale *= p.ScaleMult;
	}

	public override void ModifyWeaponKnockback(Item item, Player player, ref StatModifier knockback)
	{
		if (WeaponForgeCatalog.Get(item.type) is WeaponForgeProfile p) knockback *= p.KnockbackMult;
	}

	public override void ModifyShootStats(Item item, Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
	{
		if (WeaponForgeCatalog.Get(item.type) is WeaponForgeProfile p) velocity *= p.VelocityMult;
	}

	public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
	{
		if (WeaponForgeCatalog.Get(item.type) is not WeaponForgeProfile p) return;
		foreach ((string name, string text) in Describe(p, Main.LocalPlayer))
			tooltips.Add(new TooltipLine(Mod, name, text) { OverrideColor = LineColor });
	}

	internal static IEnumerable<(string Name, string Text)> Describe(WeaponForgeProfile p, Player viewer)
	{
		var parts = new List<string> { Language.GetTextValue(Key + "Damage", Percent(p.DamageMult)) };
		if (p.CritBonus > 0) parts.Add(Language.GetTextValue(Key + "Crit", p.CritBonus));
		if (p.SpeedMult > 1f) parts.Add(Language.GetTextValue(Key + "Speed", Percent(p.SpeedMult)));
		if (p.KnockbackMult > 1f) parts.Add(Language.GetTextValue(Key + "Knockback", Percent(p.KnockbackMult)));
		if (p.VelocityMult > 1f) parts.Add(Language.GetTextValue(Key + "Velocity", Percent(p.VelocityMult)));
		if (p.ScaleMult > 1f) parts.Add(Language.GetTextValue(Key + "Reach", Percent(p.ScaleMult)));
		yield return ("DingDongForge", Language.GetTextValue(Key + "Header", string.Join(" · ", parts)));
		if (p.Traits.Count > 0)
			yield return ("DingDongForgeTraits", Language.GetTextValue(Key + "Traits",
				string.Join(", ", p.Traits.Select(t => Language.GetTextValue(Key + "Trait." + t)))));
		if (p.TargetBoss.Length > 0)
			yield return ("DingDongForgeBosses", Language.GetTextValue(Key + "Bosses", p.TargetBoss, p.TargetRatio.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)));
		if (p.Attunement != WeaponForgeBiome.None)
		{
			bool active = viewer != null && viewer.active && WeaponForgeTraits.InBiome(viewer, p.Attunement);
			yield return ("DingDongForgeBiome", Language.GetTextValue(Key + (active ? "AttunedActive" : "Attuned"),
				Language.GetTextValue(Key + "Biome." + p.Attunement), (int)Math.Round(WeaponForgeCatalog.AttunementBonus * 100f)));
		}
	}

	private static int Percent(float mult) => (int)Math.Round((mult - 1f) * 100f);
}
