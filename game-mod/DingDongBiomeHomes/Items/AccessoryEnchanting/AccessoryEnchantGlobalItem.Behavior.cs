using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.AccessoryEnchanting;
public sealed partial class AccessoryEnchantGlobalItem
{
	public override GlobalItem Clone(Item from, Item to)
	{
		AccessoryEnchantGlobalItem clone = (AccessoryEnchantGlobalItem)base.Clone(from, to);
		clone.Level = Level;
		clone.Imprint = Imprint;
		clone.fused = new List<AccessoryEnchantmentId>(fused);
		return clone;
	}

	public override void SetDefaults(Item entity)
	{
		Level = 0;
		Imprint = AccessoryEnchantmentId.None;
		fused.Clear();
	}

	public override void PostReforge(Item item)
	{
		if (!item.accessory || item.IsAir)
			return;
		IReadOnlyList<AccessoryEnchantmentId> all = AccessoryEnchantmentCatalog.All;
		Imprint = all[Main.rand.Next(all.Count)];
	}

	public override bool CanStack(Item destination, Item source)
	{
		if (!destination.TryGetGlobalItem(out AccessoryEnchantGlobalItem destinationState) || !source.TryGetGlobalItem(out AccessoryEnchantGlobalItem sourceState))
			return true;
		return destinationState.Level == sourceState.Level && destinationState.Imprint == sourceState.Imprint && destinationState.fused.SequenceEqual(sourceState.fused);
	}

	public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
	{
		if (!item.accessory)
			return;
		string slots = string.Join(", ", fused.Select(value => AccessoryEnchantmentCatalog.Get(value).Name));
		tooltips.Add(new TooltipLine(Mod, "AccessoryLevel", AccessoryEnchantingLocalization.Text("Tooltips.Upgrade", Level, MaxLevel, fused.Count, Level)));
		if (Imprint != AccessoryEnchantmentId.None)
		{
			AccessoryEnchantmentDefinition imprint = AccessoryEnchantmentCatalog.Get(Imprint);
			tooltips.Add(new TooltipLine(Mod, "AccessoryImprint", AccessoryEnchantingLocalization.Text("Tooltips.Imprint", imprint.Name, imprint.Effect)) { OverrideColor = new Microsoft.Xna.Framework.Color(240, 190, 80) });
		}

		if (fused.Count > 0)
			tooltips.Add(new TooltipLine(Mod, "AccessoryFused", AccessoryEnchantingLocalization.Text("Tooltips.Fused", slots)) { OverrideColor = new Microsoft.Xna.Framework.Color(100, 220, 205) });
	}

	internal bool TryAdvanceLevel()
	{
		if (Level >= MaxLevel)
			return false;
		Level++;
		return true;
	}

	internal bool TryAddFused(AccessoryEnchantmentId enchantment)
	{
		if (!AccessoryEnchantmentCatalog.IsValid(enchantment) || fused.Count >= Level || fused.Contains(enchantment))
			return false;
		fused.Add(enchantment);
		return true;
	}
}
