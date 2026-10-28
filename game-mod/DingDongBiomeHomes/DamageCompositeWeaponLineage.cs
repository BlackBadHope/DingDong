using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

/// <summary>
/// Composite weapons (a weapon forged from other weapons, Night's Edge being
/// the original case) inherit the biome element of every source weapon and
/// apply those on-hit statuses together with their own element.
///
/// Vanilla composites are pinned to their canonical source blades so content
/// mods that rewrite a vanilla recipe never strip or reshuffle the bonuses.
/// Every other composite is discovered once from the loaded recipe graph: a
/// weapon whose recipe consumes two or more weapons (the Broken Hero Sword
/// counts as a hero blade), or one weapon that is itself a composite. The
/// table depends only on loaded content and recipes, which tModLoader keeps
/// identical on the server and every client, so no state is synced: hits keep
/// going through the ordinary native NPC buff sync.
/// </summary>
public static class CompositeWeaponLineage
{
	public readonly record struct InheritedElement(string Element, int SourceItemType);

	/// <summary>Upper bound of inherited elements, which also bounds the
	/// number of statuses a single hit may apply (NPC buff slots are shared
	/// with whip tags and other mods' debuffs).</summary>
	public const int MaxInheritedElements = 6;

	private static readonly InheritedElement[] None = Array.Empty<InheritedElement>();
	private static InheritedElement[][] table;

	private static readonly (int Result, int[] Sources)[] PinnedVanilla =
	{
		(ItemID.NightsEdge, new int[] { ItemID.LightsBane, ItemID.Muramasa, ItemID.BladeofGrass, ItemID.FieryGreatsword }),
		(ItemID.TrueNightsEdge, new int[] { ItemID.NightsEdge, ItemID.BrokenHeroSword }),
		(ItemID.TrueExcalibur, new int[] { ItemID.Excalibur, ItemID.BrokenHeroSword }),
		(ItemID.TerraBlade, new int[] { ItemID.TrueNightsEdge, ItemID.TrueExcalibur, ItemID.BrokenHeroSword }),
		(ItemID.Zenith, new int[] { ItemID.TerraBlade, ItemID.Meowmere, ItemID.StarWrath, ItemID.InfluxWaver, ItemID.TheHorsemansBlade,
			ItemID.Seedler, ItemID.Starfury, ItemID.BeeKeeper, ItemID.EnchantedSword, ItemID.CopperShortsword })
	};

	/// <summary>
	/// Biome of origin for vanilla source blades whose internal name carries
	/// no element word. Only used as an inherited contribution; the blades'
	/// own persisted element tags stay untouched.
	/// </summary>
	internal static string SourceContribution(int itemType) => itemType switch
	{
		ItemID.LightsBane or ItemID.BloodButcherer => DamageTags.Shadow, // Corruption / Crimson
		ItemID.BladeofGrass => DamageTags.Poison,                         // Jungle venom
		ItemID.FieryGreatsword => DamageTags.Fire,                         // Underworld
		ItemID.Excalibur => DamageTags.Holy,                               // Hallow
		ItemID.InfluxWaver => DamageTags.Lightning,                        // Martian technology
		ItemID.Seedler => DamageTags.Nature,                               // Jungle temple flora
		_ => SampleElement(itemType)
	};

	/// <summary>Inherited elements of <paramref name="itemType"/> in source
	/// order; empty for anything that is not a composite weapon. No allocation.</summary>
	public static InheritedElement[] Get(int itemType)
	{
		InheritedElement[][] current = table ?? Build();
		return itemType > 0 && itemType < current.Length ? current[itemType] : None;
	}

	public static bool IsComposite(int itemType) => Get(itemType).Length > 0;

	internal static void Reset() => table = null;

	internal static InheritedElement[][] Build()
	{
		int count = ItemLoader.ItemCount;
		int[][] sources = new int[count][];
		int[] best = new int[count];
		List<int> scratch = new();
		for (int index = 0; index < Recipe.numRecipes; index++)
		{
			Recipe recipe = Main.recipe[index];
			if (recipe?.createItem is null || recipe.createItem.IsAir || recipe.Disabled)
				continue;
			int result = recipe.createItem.type;
			if (result <= 0 || result >= count || !IsWeapon(result))
				continue;
			scratch.Clear();
			foreach (Item ingredient in recipe.requiredItem)
			{
				if (ingredient is null || ingredient.IsAir || ingredient.type == result || scratch.Contains(ingredient.type))
					continue;
				if (ingredient.type == ItemID.BrokenHeroSword || IsWeapon(ingredient.type))
					scratch.Add(ingredient.type);
			}
			// Several recipes for one result: the one with the most source
			// weapons wins, ties keep the earliest recipe (deterministic).
			if (scratch.Count > best[result])
			{
				best[result] = scratch.Count;
				sources[result] = scratch.ToArray();
			}
		}
		foreach ((int result, int[] pinned) in PinnedVanilla)
			if (result < count)
				sources[result] = pinned;

		InheritedElement[][] resolved = new InheritedElement[count][];
		byte[] state = new byte[count];
		for (int type = 0; type < count; type++)
			Resolve(type, sources, resolved, state);
		table = resolved;
		return resolved;
	}

	private static InheritedElement[] Resolve(int type, int[][] sources, InheritedElement[][] resolved, byte[] state)
	{
		if (type <= 0 || type >= resolved.Length)
			return None;
		if (state[type] == 2)
			return resolved[type];
		if (state[type] == 1)
			return None; // recipe cycle (conversion recipes): no inheritance through it
		state[type] = 1;
		InheritedElement[] result = None;
		int[] direct = sources[type];
		if (direct is { Length: > 0 })
		{
			bool composite = direct.Length >= 2;
			foreach (int source in direct)
				composite |= Resolve(source, sources, resolved, state).Length > 0;
			if (composite)
			{
				List<InheritedElement> list = new(MaxInheritedElements);
				foreach (int source in direct)
				{
					// Deeper lineage first, so each element is credited to the
					// original biome blade rather than the intermediate composite.
					foreach (InheritedElement deeper in Resolve(source, sources, resolved, state))
						AddDistinct(list, deeper.Element, deeper.SourceItemType);
					AddDistinct(list, SourceContribution(source), source);
				}
				if (list.Count > 0)
					result = list.ToArray();
			}
		}
		resolved[type] = result;
		state[type] = 2;
		return result;
	}

	private static void AddDistinct(List<InheritedElement> list, string element, int source)
	{
		if (list.Count >= MaxInheritedElements || string.IsNullOrEmpty(element)
			|| element == DamageTags.Neutral || element == DamageTags.ModElement)
			return;
		foreach (InheritedElement existing in list)
			if (existing.Element == element)
				return;
		list.Add(new InheritedElement(element, source));
	}

	private static string SampleElement(int itemType) =>
		ContentSamples.ItemsByType.TryGetValue(itemType, out Item sample) && sample.TryGetGlobalItem(out DamageAffinityGlobalItem affinity)
			? affinity.ElementTag
			: DamageTags.Neutral;

	/// <summary>A real weapon: deals damage, is used, is not ammo, a consumable,
	/// an accessory, a placeable or a mining/chopping tool.</summary>
	internal static bool IsWeapon(int itemType)
	{
		if (!ContentSamples.ItemsByType.TryGetValue(itemType, out Item sample))
			return false;
		return sample.damage > 0 && sample.useStyle != ItemUseStyleID.None && !sample.consumable && sample.ammo == AmmoID.None
			&& !sample.accessory && sample.createTile < 0 && sample.pick == 0 && sample.axe == 0 && sample.hammer == 0;
	}
}

/// <summary>Builds the composite table once the recipe graph is final.</summary>
public sealed class CompositeWeaponLineageSystem : ModSystem
{
	public override void PostSetupRecipes() => CompositeWeaponLineage.Build();

	public override void Unload() => CompositeWeaponLineage.Reset();
}

/// <summary>Lists the inherited biome bonuses of a composite weapon.</summary>
public sealed class CompositeWeaponTooltipGlobalItem : GlobalItem
{
	private static readonly Color LineColor = new(186, 168, 255);

	public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
	{
		CompositeWeaponLineage.InheritedElement[] inherited = CompositeWeaponLineage.Get(item.type);
		if (inherited.Length == 0)
			return;
		tooltips.Add(new TooltipLine(Mod, "DingDongCompositeHeader",
			Language.GetTextValue("Mods.DingDongBiomeHomes.ElementalLineage.Header")) { OverrideColor = LineColor });
		for (int i = 0; i < inherited.Length; i++)
		{
			string effect = Language.GetTextValue("Mods.DingDongBiomeHomes.ElementalLineage.Effects." + DamageElementalStatusPolicy.EffectKey(inherited[i].Element));
			string line = Language.GetTextValue("Mods.DingDongBiomeHomes.ElementalLineage.Line",
				Lang.GetItemNameValue(inherited[i].SourceItemType), DamageUiText.Tag(inherited[i].Element), effect);
			tooltips.Add(new TooltipLine(Mod, "DingDongCompositeElement" + i, line) { OverrideColor = LineColor });
		}
	}
}
