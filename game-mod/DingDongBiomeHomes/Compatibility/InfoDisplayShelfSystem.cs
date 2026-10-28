using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Compatibility;

/// <summary>
/// Owner acceptance item 3 (28.10): with the open inventory, vanilla stacks the
/// info-accessory icons (watch, DPS meter, Thorium HPS, Fargo wing time, ...)
/// in a column just left of the three vanilla equipment columns. Every extra
/// modded accessory column (Fargo EMode, WingSlotExtra, Calamity) is laid out
/// 191 px further left by tModLoader, exactly over that column, so the icons
/// cover the slots. The icons are moved onto a shelf under the whole grid,
/// left of the defense icon, which tModLoader keeps below every column.
/// Hover and click rectangles come from the same positions, so toggling and
/// the page arrow keep working.
/// </summary>
public sealed class InfoDisplayShelfSystem : ModSystem
{
	internal const int Pitch = 24;
	internal const int GapFromDefenseIcon = 12;
	internal const int ShelfDrop = 4;
	internal const int IconsPerPage = 12;

	public override void Load()
	{
		if (!Main.dedServ)
			On_Main.GetInfoAccIconPosition += Shelf;
	}

	public override void Unload()
	{
		if (!Main.dedServ)
			On_Main.GetInfoAccIconPosition -= Shelf;
	}

	internal static Point ShelfPosition(Vector2 defenseIcon, int drawnCount)
	{
		int slot = Math.Clamp(drawnCount, 0, IconsPerPage - 1);
		return new Point((int)defenseIcon.X - GapFromDefenseIcon - Pitch * (slot + 1), (int)defenseIcon.Y + ShelfDrop);
	}

	private static void Shelf(On_Main.orig_GetInfoAccIconPosition orig, int drawnCount, int startX, out int X, out int Y)
	{
		orig(drawnCount, startX, out X, out Y);
		if (!Main.playerInventory)
			return;
		Vector2 defense = AccessorySlotLoader.DefenseIconPosition;
		if (defense == Vector2.Zero)
			return; // equipment grid not drawn yet this session: keep vanilla
		Point shelf = ShelfPosition(defense, drawnCount);
		X = shelf.X;
		Y = shelf.Y;
	}

	internal static int RunFormulaChecks()
	{
		Vector2 defense = new(1828, 770);
		Point first = ShelfPosition(defense, 0), last = ShelfPosition(defense, 11), arrow = ShelfPosition(defense, 11);
		if (first.Y != 774 || first.X != 1828 - 12 - 24) throw new InvalidOperationException("Shelf first slot misplaced.");
		if (last.X != 1828 - 12 - 24 * 12 || last.X + 22 >= first.X - Pitch + 22 + 1 && first.X - last.X != Pitch * 11) throw new InvalidOperationException("Shelf pitch broken.");
		if (ShelfPosition(defense, 40) != arrow) throw new InvalidOperationException("Shelf must clamp to one page.");
		if (last.Y != first.Y) throw new InvalidOperationException("Shelf must be a single row.");
		return 4;
	}
}
