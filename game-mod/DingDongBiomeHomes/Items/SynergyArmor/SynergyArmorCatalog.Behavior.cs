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
	internal static void Register(Mod mod)
	{
		if (Sets.Count != 0)
			throw new InvalidOperationException("Synergy armor content was registered twice.");
		if (Definitions.Count != ExpectedSetCount || Definitions.Select(definition => definition.Id).Distinct().Count() != ExpectedSetCount)
			throw new InvalidOperationException("Synergy armor definition count or identity is invalid.");
		foreach (SynergyArmorSetDefinition definition in Definitions)
		{
			GeneratedSynergyArmorItem[] items = Enum.GetValues<SynergyArmorPiece>().Select(piece => new GeneratedSynergyArmorItem(definition, piece)).ToArray();
			foreach (GeneratedSynergyArmorItem item in items)
			{
				if (!mod.AddContent(item))
					throw new InvalidOperationException($"Failed to register synergy armor item {item.Name}.");
				EquipType equipType = item.Piece switch
				{
					SynergyArmorPiece.Head => EquipType.Head,
					SynergyArmorPiece.Body => EquipType.Body,
					_ => EquipType.Legs
				};
				EquipLoader.AddEquipTexture(mod, $"{item.Texture}_{item.Piece}", equipType, item);
			}

			Sets.Add(definition.Id, new SynergyArmorSet(definition, items));
			DefinitionsByKey.Add(definition.Key, definition);
		}
	}

	internal static bool TryGetDefinition(string key, out SynergyArmorSetDefinition definition) => DefinitionsByKey.TryGetValue(key, out definition);
	private static SynergyArmorIngredient I(int type, int head, int body, int legs) => new(type, head, body, legs);
	private static SynergyArmorSetDefinition D(SynergyArmorSetId id, string key, string displayName, SynergyArmorBand band, string acquisition, string setBonus, string hiddenHint, string[] pieceNames, int[] defense, int rarity, int value, int tile, SynergyArmorIngredient[] ingredients, string relicSource = null, MobRelicTier? relicTier = null) => new(id, key, displayName, band, acquisition, setBonus, hiddenHint, pieceNames, defense, rarity, value, tile, ingredients, relicSource, relicTier);
}
