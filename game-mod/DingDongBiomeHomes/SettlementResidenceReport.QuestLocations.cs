using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes;

public sealed partial class SettlementResidenceReport
{
	private Point[] questLocations;
	internal Point QuestPoint(bool patrol, int variant)
	{
		EnsureQuestLocations();
		return questLocations[(patrol ? 8 : 0) + Math.Clamp(variant, 0, 7)];
	}
	private int[] QuestLocationCoordinates()
	{
		EnsureQuestLocations();
		return questLocations.SelectMany(p => new[] { p.X, p.Y }).ToArray();
	}
	private void ReadQuestCoordinates(int[] values)
	{
		if (values.Length != 32) throw new InvalidDataException("Invalid residence quest location count.");
		var points = new Point[16];
		var allowed = Area; allowed.Inflate(8, 8);
		for (int i = 0; i < points.Length; i++)
		{
			points[i] = new Point(values[i * 2], values[i * 2 + 1]);
			if (!allowed.Contains(points[i])) throw new InvalidDataException("Quest location outside residence envelope.");
		}
		questLocations = points;
	}
	internal static bool CanStandAtQuestPoint(Point p)
	{
		if (!WorldGen.InWorld(p.X, p.Y, 4)) return false;
		// Center of a normal 20x42 player, with a floor under both feet.
		Vector2 top = new(p.X * 16 + 8 - 10, p.Y * 16 + 8 - 21);
		if (Collision.SolidCollision(top, 20, 42)) return false;
		int feet = p.Y + 2; // 3px below the player's feet; gravity settles it.
		bool support = false;
		for (int x = p.X - 1; x <= p.X + 1; x++)
		{
			Tile tile = Main.tile[x, feet];
			if (tile.LiquidAmount > 0 && tile.LiquidType == LiquidID.Lava) return false;
			support |= tile.HasTile && !tile.IsActuated && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]);
		}
		return support;
	}
	private void EnsureQuestLocations()
	{
		if (questLocations is not null) return;
		// Clients receive authoritative points with the registry, before remote
		// terrain sections arrive. Never infer a target from missing client tiles.
		if (Main.netMode == NetmodeID.MultiplayerClient)
			throw new InvalidOperationException("Missing authoritative residence quest locations.");
		List<Point> stands = new();
		List<Point> entrances = new();
		var search = Area; search.Inflate(6, 6);
		for (int x = Math.Max(4, search.Left); x < Math.Min(Main.maxTilesX - 4, search.Right); x++)
		for (int y = Math.Max(4, search.Top); y < Math.Min(Main.maxTilesY - 4, search.Bottom); y++)
		{
			Point p = new(x, y);
			if (CanStandAtQuestPoint(p)) stands.Add(p);
			Tile tile = Main.tile[x, y];
			if (!tile.HasTile || !Area.Contains(p) || tile.TileType is not (TileID.ClosedDoor or TileID.OpenDoor or TileID.TallGateClosed or TileID.TallGateOpen)) continue;
			for (int side = -2; side <= 2; side += 4)
			for (int dy = -2; dy <= 2; dy++)
			{
				Point beside = new(x + side, y + dy);
				if (CanStandAtQuestPoint(beside)) entrances.Add(beside);
			}
		}
		if (stands.Count == 0) throw new InvalidOperationException("No standing quest locations for " + NpcKey);
		static long Distance(Point a, Point b) { long x = a.X - b.X, y = a.Y - b.Y; return x * x + y * y; }
		Point Nearest(Point desired) => stands.MinBy(p => Distance(p, desired));
		questLocations = new Point[16];
		for (int i = 0; i < 8; i++)
		{
			questLocations[i] = Nearest(NpcQuestSystem.SurveyReferencePoint(Area, i));
			questLocations[i + 8] = Nearest(NpcQuestSystem.PatrolReferencePoint(Area, i));
		}
		if (entrances.Count > 0)
			questLocations[7] = entrances.OrderBy(p => Main.wallHouse[Main.tile[p.X, p.Y].WallType] ? 1 : 0)
				.ThenBy(p => Distance(p, Area.Center)).First();
		else
			questLocations[7] = stands.MinBy(p => Math.Min(Math.Abs(p.X - Area.Left), Math.Abs(p.X - Area.Right)) + Math.Abs(p.Y - Area.Center.Y));
	}
}
