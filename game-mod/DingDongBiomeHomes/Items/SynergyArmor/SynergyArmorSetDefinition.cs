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
internal sealed record SynergyArmorSetDefinition(SynergyArmorSetId Id, string Key, string DisplayName, SynergyArmorBand Band, string Acquisition, string SetBonus, string HiddenHint, string[] PieceNames, int[] Defense, int Rarity, int Value, int CraftingTile, SynergyArmorIngredient[] Ingredients, string RelicSource = null, MobRelicTier? RelicTier = null)
{
	internal string InternalName(SynergyArmorPiece piece) => $"SynergyArmor_{Key}_{piece}";
	internal string DisplayNameFor(SynergyArmorPiece piece) => $"{Localized("Name", DisplayName)} {Localized($"Pieces.{piece}", PieceNames[(int)piece])}";
	internal int DefenseFor(SynergyArmorPiece piece) => Defense[(int)piece];
	internal string LocalizedSetBonus() => Localized("Bonus", SetBonus);
	internal string LocalizedSynergy() => Localized("Synergy", HiddenHint);
	internal string LocalizedAcquisition() => Localized("Acquisition", Acquisition);
	private string Localized(string suffix, string fallback)
	{
		string key = $"Mods.DingDongBiomeHomes.SynergyArmor.Sets.{Key}.{suffix}";
		string value = Language.GetTextValue(key);
		return value == key ? fallback : value;
	}
}
