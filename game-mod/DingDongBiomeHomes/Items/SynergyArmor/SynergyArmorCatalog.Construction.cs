using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DingDongBiomeHomes.Items.MobRelics;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.SynergyArmor;
internal static partial class SynergyArmorCatalog
{
	private static IReadOnlyList<SynergyArmorSetDefinition> BuildDefinitions()
	{
		var definitions = new List<SynergyArmorSetDefinition>(ExpectedSetCount);
		definitions.AddRange(BuildFrontierDefinitions());
		definitions.AddRange(BuildPreHardmodeDefinitions());
		definitions.AddRange(BuildEarlyHardmodeDefinitions());
		definitions.AddRange(BuildLateHardmodeDefinitions());
		definitions.AddRange(BuildEndgameDefinitions());
		return definitions;
	}

	private static IReadOnlyList<SynergyArmorSetDefinition> BuildFrontierDefinitions() => [D(SynergyArmorSetId.ThresholdForager, "ThresholdForager", "Threshold Forager", SynergyArmorBand.Frontier, "Frontier craft: wood, gel and fallen stars at a work bench.", "Travel charges a living threshold; the next hit plants a healing briar echo.", "Verdant traces remember every tile crossed.", ["Cap", "Coat", "Treads"], [2, 3, 2], ItemRarityID.Blue, Item.sellPrice(silver: 35), TileID.WorkBenches, [I(ItemID.Wood, 10, 20, 15), I(ItemID.Gel, 5, 10, 8), I(ItemID.FallenStar, 1, 1, 1)]), D(SynergyArmorSetId.PrismGlasswright, "PrismGlasswright", "Prism Glasswright", SynergyArmorBand.Frontier, "Frontier craft: cactus, glass and lenses at a furnace.", "Critical hits cut prism facets; three facets release a three-way refracted strike.", "Radiant relics teach the glass to keep the color it steals.", ["Visor", "Cuirass", "Sandals"], [3, 4, 3], ItemRarityID.Blue, Item.sellPrice(silver: 60), TileID.Furnaces, [I(ItemID.Cactus, 15, 25, 20), I(ItemID.Glass, 8, 12, 10), I(ItemID.Lens, 1, 2, 1)]), D(SynergyArmorSetId.BurrowBell, "BurrowBell", "Buried Bell", SynergyArmorBand.Frontier, "Frontier elite craft: desert fossils, mandibles and amber at an anvil.", "Stillness winds a buried bell; the next hit releases a defensive ground pulse.", "Tidal resonance can flow through stone without breaking the charge.", ["Helm", "Harness", "Greaves"], [4, 5, 4], ItemRarityID.Green, Item.sellPrice(gold: 1), TileID.Anvils, [I(ItemID.DesertFossil, 12, 20, 16), I(ItemID.AntlionMandible, 2, 3, 2), I(ItemID.Amber, 1, 2, 1)])];
}
