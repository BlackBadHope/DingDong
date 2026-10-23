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
	private static IReadOnlyList<SynergyArmorSetDefinition> BuildEndgameDefinitions() => [D(SynergyArmorSetId.LunarCartel, "LunarCartel", "Lunar Cartel", SynergyArmorBand.Endgame, "Pillar craft: all four celestial fragments and a Grand Thunder Bird Curio.", "Hit with melee, ranged, magic and summon damage: gain +15% damage for 10 seconds and launch four 28% echoes.", "Relics from four different mods extend the commission from 10 to 15 seconds.", ["Seal", "Consortium", "Commissions"], [18, 22, 16], ItemRarityID.Red, Item.sellPrice(gold: 14), TileID.LunarCraftingStation, [I(ItemID.FragmentSolar, 6, 10, 8), I(ItemID.FragmentVortex, 6, 10, 8), I(ItemID.FragmentNebula, 6, 10, 8), I(ItemID.FragmentStardust, 6, 10, 8)], "ThoriumMod/TheGrandThunderBird", MobRelicTier.CommonCurio), D(SynergyArmorSetId.MoonlordThreshold, "MoonlordThreshold", "Moonlord Threshold", SynergyArmorBand.Endgame, "Post-Moon Lord craft: luminite, celestial fragments and an Eye of Cthulhu Curio.", "With Relic Echo active, every third hit creates two delayed echoes at 22% damage; Threshold Ward reflects 80% of a wound once per second.", "Full Convergence raises the set bonus to +18% damage, creates three 32% echoes and raises reflection to 120%.", ["Crown", "Boundary", "Crossing"], [20, 26, 18], ItemRarityID.Red, Item.sellPrice(gold: 18), TileID.LunarCraftingStation, [I(ItemID.LunarBar, 8, 14, 10), I(ItemID.FragmentSolar, 4, 6, 5), I(ItemID.FragmentVortex, 4, 6, 5), I(ItemID.FragmentNebula, 4, 6, 5), I(ItemID.FragmentStardust, 4, 6, 5)], "Terraria/EyeofCthulhu", MobRelicTier.CommonCurio), D(SynergyArmorSetId.LegendaryBellArchitect, "LegendaryBellArchitect", "Legendary Bell Architect", SynergyArmorBand.Endgame, "Legendary-world apex craft: heavy luminite, every fragment and a Glowmoth Myth for each piece.", "Cycles every 5 seconds: Offense (+18% damage, +8% crit), Shelter (+14 defense, 8% reduction, regen, knockback immunity), Passage (+18% speed, jump and run speed).", "Three relic providers plus Full Convergence activate all three phases at once; its strike then triggers every third hit for 125% instead of every fifth for 75%.", ["Belfry", "Cathedral", "Pilgrimage"], [24, 30, 22], ItemRarityID.Red, Item.sellPrice(gold: 25), TileID.LunarCraftingStation, [I(ItemID.LunarBar, 18, 30, 24), I(ItemID.FragmentSolar, 12, 20, 16), I(ItemID.FragmentVortex, 12, 20, 16), I(ItemID.FragmentNebula, 12, 20, 16), I(ItemID.FragmentStardust, 12, 20, 16)], "SOTS/Glowmoth", MobRelicTier.LegendaryMyth)];
}
