using System;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static partial class WorldCompilerProgressionChests
{
	private static readonly int[] Dungeon = [ItemID.Muramasa, ItemID.CobaltShield,
		ItemID.AquaScepter, ItemID.MagicMissile, ItemID.Handgun, ItemID.BlueMoon,
		ItemID.ShadowKey, ItemID.Valor];
	private static readonly int[] Jungle = [ItemID.AnkletoftheWind,
		ItemID.FeralClaws, ItemID.Boomstick, ItemID.StaffofRegrowth];
	private static readonly int[] Sky = [ItemID.Starfury, ItemID.ShinyRedBalloon,
		ItemID.LuckyHorseshoe, ItemID.CelestialMagnet];
	private static readonly int[] Hell = [ItemID.Sunfury, ItemID.Flamelash,
		ItemID.DarkLance, ItemID.HellwingBow, ItemID.FlowerofFire];
	private static readonly int[] Pyramid = [ItemID.SandstorminaBottle,
		ItemID.FlyingCarpet, ItemID.PharaohsMask];

	internal static void Place(WorldCompilerPlan plan, WorldCompilerProgressionReceipt receipt)
	{
		receipt.PyramidLootPending = true;
		int dungeon = 0, jungle = 0, sky = 0, hell = 0, temple = 0;
		foreach (WorldCompilerRoom room in plan.Structures.Rooms)
		{
			int primary, style; string kind;
			switch (room.Kind)
			{
				case WorldCompilerStructureKind.DungeonRoom:
					if (dungeon++ % 3 != 0) continue;
					primary = Dungeon[(dungeon / 3) % Dungeon.Length]; style = 2; kind = "dungeon"; break;
				case WorldCompilerStructureKind.JungleShrine:
					primary = Jungle[jungle++ % Jungle.Length]; style = 10; kind = "jungle"; break;
				case WorldCompilerStructureKind.FloatingIsland:
					primary = Sky[sky++ % Sky.Length]; style = 13; kind = "sky"; break;
				case WorldCompilerStructureKind.UnderworldForge:
					primary = Hell[hell++ % Hell.Length]; style = 4; kind = "underworld"; break;
				case WorldCompilerStructureKind.DesertPyramid:
					continue; // Fargo excavates its arena/passages before pyramid loot placement.
				case WorldCompilerStructureKind.TempleRoom:
					if (temple++ % 10 != 0) continue;
					primary = ItemID.LihzahrdPowerCell; style = 16; kind = "temple"; break;
				default: continue;
			}
			int x = room.Kind == WorldCompilerStructureKind.FloatingIsland ?
				room.CenterX : room.Bounds.Right - 9;
			int y = room.Kind == WorldCompilerStructureKind.FloatingIsland ?
				room.Bounds.Y - 1 : room.Bounds.Bottom - 4;
			if (room.Kind == WorldCompilerStructureKind.DesertPyramid) y--;
			if (room.Kind == WorldCompilerStructureKind.FloatingIsland)
				(x, y) = IslandSite(room);
			PlaceAt(kind, x, y, primary, style, receipt);
		}
	}

	private static (int X, int Y) IslandSite(WorldCompilerRoom room)
	{
		for (int x = room.CenterX - 24; x < room.CenterX + 24; x++)
		for (int y = room.Bounds.Y; y < room.CenterY; y++)
			if (Main.tile[x, y].HasTile && Main.tile[x + 1, y].HasTile &&
				!Main.tile[x, y - 1].HasTile && !Main.tile[x + 1, y - 1].HasTile)
				return (x, y - 1);
		throw new InvalidOperationException("Floating island has no supported chest site.");
	}
}
