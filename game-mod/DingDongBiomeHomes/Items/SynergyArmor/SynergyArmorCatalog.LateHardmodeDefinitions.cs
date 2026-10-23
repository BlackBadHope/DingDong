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
	private static IReadOnlyList<SynergyArmorSetDefinition> BuildLateHardmodeDefinitions() => [D(SynergyArmorSetId.RootclockGardener, "RootclockGardener", "Rootclock Gardener", SynergyArmorBand.LateHardmode, "Post-Plantera craft: chlorophyte, life fruit, jungle spores and a Crabulon Curio.", "Damage planted in one target grows for three seconds, then blooms as stored damage and healing.", "Verdant and Tidal aspects disagree about time but agree about growth.", ["Bloom", "Greenhouse", "Rootwalkers"], [14, 16, 12], ItemRarityID.Lime, Item.sellPrice(gold: 8), TileID.MythrilAnvil, [I(ItemID.ChlorophyteBar, 10, 18, 14), I(ItemID.LifeFruit, 1, 2, 1), I(ItemID.JungleSpores, 8, 12, 10)], "CalamityMod/Crabulon", MobRelicTier.CommonCurio), D(SynergyArmorSetId.TempleParadox, "TempleParadox", "Temple Paradox", SynergyArmorBand.LateHardmode, "Post-Golem craft: beetle husks, chlorophyte, power cells and a Glowmoth Curio.", "Part of each wound becomes time debt; surviving the interval refunds it and retaliates.", "Radiant and Umbral relics together make the contradiction profitable.", ["Dial", "Epoch", "Steps"], [16, 20, 14], ItemRarityID.Yellow, Item.sellPrice(gold: 10), TileID.MythrilAnvil, [I(ItemID.BeetleHusk, 4, 8, 6), I(ItemID.ChlorophyteBar, 10, 16, 12), I(ItemID.LihzahrdPowerCell, 1, 2, 1)], "SOTS/Glowmoth", MobRelicTier.CommonCurio), D(SynergyArmorSetId.EventHorizonMason, "EventHorizonMason", "Event Horizon Mason", SynergyArmorBand.LateHardmode, "Post-Plantera elite craft: ectoplasm, shroomite and souls of fright.", "Hits add mass; critical mass pulls enemies inward and collapses into an area strike.", "Umbral resonance makes empty space heavier than matter.", ["Lens", "Mantle", "Gravboots"], [15, 18, 13], ItemRarityID.Cyan, Item.sellPrice(gold: 11), TileID.MythrilAnvil, [I(ItemID.Ectoplasm, 10, 18, 14), I(ItemID.ShroomiteBar, 8, 14, 10), I(ItemID.SoulofFright, 5, 8, 6)])];
}
