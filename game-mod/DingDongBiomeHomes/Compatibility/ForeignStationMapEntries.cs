using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Compatibility;

/// <summary>
/// Owner 28.10 04:40: Recipe Browser showed a crafting station as
/// "BaitWorkshopMods.RecipeBrowser.RecipeBrowserUI.NoTileEntry". The tile
/// (UnuBattleRods' Bait Workshop) has no map entry, so the browser falls back to
/// the internal tile name plus a localized suffix whose key Recipe Browser
/// itself misspells (our hjson now supplies that key). This pass closes the
/// root cause for the whole pack: every modded tile from another mod that an
/// item places but that registered no map entry gets one, named after the
/// placing item, before tModLoader seals the map legend. The tiles then show
/// on the minimap and the browser prints their real names.
/// </summary>
public sealed class ForeignStationMapEntries : ModSystem
{
	private static readonly Color DefaultEntryColor = new(150, 130, 110);

	public override void PostSetupContent()
	{
		Type loader = typeof(ModTile).Assembly.GetType("Terraria.ModLoader.MapLoader");
		FieldInfo entriesField = loader?.GetField("tileEntries", BindingFlags.NonPublic | BindingFlags.Static);
		FieldInfo initializedField = loader?.GetField("initialized", BindingFlags.NonPublic | BindingFlags.Static);
		if (entriesField?.GetValue(null) is not IDictionary entries || initializedField == null || (bool)initializedField.GetValue(null))
		{
			Mod.Logger.Warn("DINGDONG_FOREIGN_MAP_ENTRIES|verdict=SKIPPED|reason=map-loader-contract-changed");
			return;
		}

		Dictionary<int, Item> placers = new();
		foreach ((int type, Item item) in ContentSamples.ItemsByType)
		{
			if (item == null || item.createTile < TileID.Count || item.ModItem == null) continue;
			placers.TryAdd(item.createTile, item);
		}

		int added = 0, candidates = 0;
		List<string> names = new();
		foreach ((int tileType, Item item) in placers)
		{
			ModTile tile = TileLoader.GetTile(tileType);
			if (tile == null || tile.Mod == Mod) continue;
			candidates++;
			if (entries.Contains((ushort)tileType)) continue;
			tile.AddMapEntry(DefaultEntryColor, item.ModItem.DisplayName);
			added++;
			if (names.Count < 40) names.Add(tile.Mod.Name + "/" + tile.Name);
		}
		Mod.Logger.Info($"DINGDONG_FOREIGN_MAP_ENTRIES|verdict=PASS|foreignPlacedTiles={candidates}|added={added}|examples={string.Join(",", names)}");
	}
}
