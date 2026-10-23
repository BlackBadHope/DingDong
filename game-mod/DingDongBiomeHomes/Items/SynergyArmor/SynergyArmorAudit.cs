using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DingDongBiomeHomes.Items.MobRelics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.SynergyArmor;
internal static partial class SynergyArmorAudit
{
	internal static SynergyArmorAuditResult Run(Mod mod, bool requireRecipes, bool exerciseGameplay)
	{
		List<string> failures = new();
		IReadOnlyDictionary<SynergyArmorSetId, SynergyArmorSet> sets = SynergyArmorCatalog.RegisteredSets;
		List<GeneratedSynergyArmorItem> items = ModContent.GetContent<GeneratedSynergyArmorItem>().Where(item => ReferenceEquals(item.Mod, mod)).OrderBy(item => item.Definition.Id).ThenBy(item => item.Piece).ToList();
		int setsPassed = sets.Count(pair => pair.Value.Items.Count == SynergyArmorCatalog.PiecesPerSet && pair.Value.Items.Select(item => item.Piece).Distinct().Count() == SynergyArmorCatalog.PiecesPerSet);
		int bandsPassed = Enum.GetValues<SynergyArmorBand>().Count(band => SynergyArmorCatalog.AllDefinitions.Count(definition => definition.Band == band) == 3);
		int itemsPassed = 0;
		int wearableSlotsPassed = 0;
		int recipesPassed = 0;
		foreach (GeneratedSynergyArmorItem modItem in items)
		{
			Item item = new();
			item.SetDefaults(modItem.Type);
			List<TooltipLine> renderedTooltips = new();
			modItem.ModifyTooltips(renderedTooltips);
			bool tooltipValid = renderedTooltips.Count == 2 && renderedTooltips.All(line => Localized(line.Text));
			bool slotValid = modItem.Piece switch
			{
				SynergyArmorPiece.Head => item.headSlot >= 0 && item.bodySlot < 0 && item.legSlot < 0,
				SynergyArmorPiece.Body => item.bodySlot >= 0 && item.headSlot < 0 && item.legSlot < 0,
				_ => item.legSlot >= 0 && item.headSlot < 0 && item.bodySlot < 0
			};
			string inventoryAsset = $"Items/SynergyArmor/Generated/{modItem.Name}.rawimg";
			string equipAsset = $"Items/SynergyArmor/Generated/{modItem.Name}_{modItem.Piece}.rawimg";
			if (slotValid && mod.FileExists(equipAsset))
				wearableSlotsPassed++;
			bool valid = item.defense == modItem.Definition.DefenseFor(modItem.Piece) && item.defense > 0 && item.maxStack == 1 && !item.accessory && item.rare >= ItemRarityID.Blue && item.value > 0 && slotValid && string.Equals(modItem.Texture, $"DingDongBiomeHomes/Items/SynergyArmor/Generated/{modItem.Name}", StringComparison.Ordinal) && mod.FileExists(inventoryAsset) && mod.FileExists(equipAsset) && Localized(modItem.DisplayName.Value) && string.IsNullOrEmpty(modItem.Tooltip.Value) && tooltipValid;
			if (valid)
				itemsPassed++;
			else
				Failure(failures, $"item:{modItem.Name}");
			if (requireRecipes)
			{
				if (RecipeHasExactRelicContract(modItem, out string recipeFailure))
					recipesPassed++;
				else
					Failure(failures, recipeFailure);
			}
		}

		int setHooksPassed = 0;
		int runtimePassed = 0;
		int relicLinksPassed = 0;
		if (exerciseGameplay)
		{
			RunRuntimeProbe(sets, ref setHooksPassed, ref runtimePassed, ref relicLinksPassed, failures);
		}
		else
		{
			setHooksPassed = setsPassed;
			runtimePassed = setsPassed;
			relicLinksPassed = setsPassed;
		}

		bool passed = sets.Count == SynergyArmorCatalog.ExpectedSetCount && items.Count == SynergyArmorCatalog.ExpectedSetCount * SynergyArmorCatalog.PiecesPerSet && setsPassed == SynergyArmorCatalog.ExpectedSetCount && itemsPassed == SynergyArmorCatalog.ExpectedSetCount * SynergyArmorCatalog.PiecesPerSet && wearableSlotsPassed == SynergyArmorCatalog.ExpectedSetCount * SynergyArmorCatalog.PiecesPerSet && bandsPassed == Enum.GetValues<SynergyArmorBand>().Length && (!requireRecipes || recipesPassed == SynergyArmorCatalog.ExpectedSetCount * SynergyArmorCatalog.PiecesPerSet) && setHooksPassed == SynergyArmorCatalog.ExpectedSetCount && runtimePassed == SynergyArmorCatalog.ExpectedSetCount && relicLinksPassed == SynergyArmorCatalog.ExpectedSetCount && failures.Count == 0;
		SynergyArmorAuditResult result = new(passed, setsPassed, itemsPassed, wearableSlotsPassed, bandsPassed, recipesPassed, setHooksPassed, runtimePassed, relicLinksPassed, failures);
		if (passed)
			mod.Logger.Info(result.ToMarker());
		else
			mod.Logger.Error(result.ToMarker());
		return result;
	}
}
