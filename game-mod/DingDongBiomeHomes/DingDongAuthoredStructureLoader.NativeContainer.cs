using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes;

public static partial class DingDongAuthoredStructureLoader
{
	private static void ValidateRestoredSourceContext(StructureData data, StructureWritePlan plan)
	{
		if (data.Width != AuthoredSourceContext.JungleWidth || data.Height != AuthoredSourceContext.JungleHeight)
			throw new InvalidDataException("Restored source context canvas does not match its reviewed contract.");
		for (int dx = 0; dx < 3; dx++)
		for (int dy = 0; dy < 2; dy++)
		{
			int i = (AuthoredSourceContext.DresserX + dx) * data.Height + AuthoredSourceContext.DresserY + dy;
			SourceCell cell = ReadSourceCell(data, i);
			if (!plan.WriteTiles[i] || !cell.HasTile || cell.TileType != TileID.Dressers ||
				cell.State.TileFrameX != 1674 + 18 * dx || cell.State.TileFrameY != 18 * dy)
				throw new InvalidDataException("Reviewed empty source dresser is incomplete or has changed frames.");
		}
	}
	private static void PreflightNativeContainer(AuthoredPreset preset, Point origin)
	{
		if (preset.ResourcePath != AuthoredSourceContext.JungleResource) return;
		Point position = RestoredDresserPosition(origin);
		if (Chest.FindChest(position.X, position.Y) >= 0)
			throw new InvalidOperationException("Source dresser cannot replace an existing world container.");
		if (Array.FindIndex(Main.chest, chest => chest == null) < 0)
			throw new InvalidOperationException("No native chest slot available for the reviewed empty dresser.");
	}
	private static Point RestoredDresserPosition(Point origin) => new(origin.X + AuthoredSourceContext.DresserX, origin.Y + AuthoredSourceContext.DresserY);
	private static void RegisterNativeContainer(AuthoredPreset preset, Point origin)
	{
		if (preset.ResourcePath != AuthoredSourceContext.JungleResource) return;
		Point position = RestoredDresserPosition(origin);
		int slot = Chest.CreateChest(position.X, position.Y);
		if (slot < 0) throw new InvalidOperationException("Native empty dresser registration failed.");
		AuthoredNativeContainerTransaction.Record(slot, Main.chest[slot]);
		RequireNativeContainer(preset, origin, requireEmpty: true);
	}
	internal static void RequireNativeContainer(AuthoredPreset preset, Point origin, bool requireEmpty = false)
	{
		if (preset.ResourcePath != AuthoredSourceContext.JungleResource) return;
		Point position = RestoredDresserPosition(origin);
		for (int dx = 0; dx < 3; dx++)
		for (int dy = 0; dy < 2; dy++)
		{
			Tile tile = Main.tile[position.X + dx, position.Y + dy];
			if (!tile.HasTile || tile.TileType != TileID.Dressers || tile.TileFrameX != 1674 + dx * 18 || tile.TileFrameY != dy * 18)
				throw new InvalidOperationException("Restored dresser lost source geometry during framing.");
		}
		int slot = Chest.FindChest(position.X, position.Y);
		Chest chest = slot >= 0 ? Main.chest[slot] : null;
		if (chest == null || chest.item == null || chest.item.Length != Chest.maxItems ||
			requireEmpty && (chest.name != string.Empty || Array.Exists(chest.item, item => item == null || !item.IsAir)))
			throw new InvalidOperationException("Restored dresser native metadata is missing or inconsistent.");
	}
}
