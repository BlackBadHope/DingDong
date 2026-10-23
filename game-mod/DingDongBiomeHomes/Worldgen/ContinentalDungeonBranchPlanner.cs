using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen;

internal static class ContinentalDungeonBranchPlanner
{
	private const int PortalDepth = 5;
	private const int PortalHeight = 5;
	private const int ShellOverlap = 3;
	private const int MaximumBranchWidth = 1900;
	private const int MaximumBranchHeight = 360;

	internal static bool TryPlan(ContinentalDungeonEdge left, ContinentalDungeonEdge right,
		int direction, int roomCount, bool[] allowed, bool[] portalAllowed,
		IReadOnlyList<ContinentalDungeonBranchPlan> accepted,
		out ContinentalDungeonBranchPlan branch)
	{
		ContinentalDungeonEdge edge = direction < 0 ? left : right;
		if (!edge.IsValid || !TryRoot(edge, direction, allowed, portalAllowed,
			out ContinentalDungeonPlan root))
		{
			branch = null;
			return false;
		}
		var rooms = new List<ContinentalDungeonPlan>(roomCount) { root };
		while (rooms.Count < roomCount)
		{
			if (!TryNext(rooms[^1].Room, direction, allowed, out ContinentalDungeonPlan next))
			{
				branch = null;
				return false;
			}
			rooms.Add(next);
		}
		Rectangle area = rooms[0].ProtectedArea;
		for (int index = 1; index < rooms.Count; index++)
			area = Rectangle.Union(area, rooms[index].ProtectedArea);
		if (area.Width > MaximumBranchWidth || area.Height > MaximumBranchHeight ||
			OverlapsAccepted(area, accepted, 24))
		{
			branch = null;
			return false;
		}
		branch = new(rooms, area, edge, direction);
		return true;
	}

	private static bool TryRoot(ContinentalDungeonEdge edge, int direction,
		bool[] allowed, bool[] portalAllowed, out ContinentalDungeonPlan plan)
	{
		int width = WorldGen.genRand.Next(72, 113);
		int height = WorldGen.genRand.Next(34, 53);
		int gap = WorldGen.genRand.Next(10, 19);
		int boundary = edge.X;
		int roomX = direction < 0 ? boundary - gap - width : boundary + gap + 1;
		int roomY = edge.Y - height / 2 + WorldGen.genRand.Next(-6, 7);
		Rectangle room = new(roomX, roomY, width, height);
		int connectorLeft = direction < 0 ? room.Right - ShellOverlap : boundary + 1;
		int connectorRight = direction < 0 ? boundary : room.Left + ShellOverlap;
		Rectangle connector = new(connectorLeft, edge.Y - 3,
			connectorRight - connectorLeft, 7);
		int portalX = direction < 0 ? boundary : boundary - PortalDepth + 1;
		Rectangle portal = new(portalX, edge.Y - PortalHeight / 2,
			PortalDepth, PortalHeight);
		plan = new(room, connector, portal);
		if (!IsBounded(plan) || !Main.tile[edge.X, edge.Y].HasTile ||
			!ContinentalNaturalTiles.IsDungeonBrick(Main.tile[edge.X, edge.Y].TileType))
			return false;
		return GenVars.structures.CanPlace(room, allowed, 12) &&
			GenVars.structures.CanPlace(connector, allowed, 0) &&
			GenVars.structures.CanPlace(portal, portalAllowed, 0);
	}

	private static bool TryNext(Rectangle previous, int direction, bool[] allowed,
		out ContinentalDungeonPlan plan)
	{
		int width = WorldGen.genRand.Next(64, 105);
		int height = WorldGen.genRand.Next(30, 49);
		int gap = WorldGen.genRand.Next(10, 19);
		int roomX = direction < 0 ? previous.Left - gap - width : previous.Right + gap;
		int roomY = previous.Center.Y - height / 2 + WorldGen.genRand.Next(-10, 11);
		Rectangle room = new(roomX, roomY, width, height);
		int connectorLeft = direction < 0 ? room.Right - ShellOverlap : previous.Right - ShellOverlap;
		int connectorRight = direction < 0 ? previous.Left + ShellOverlap : room.Left + ShellOverlap;
		int top = Math.Min(previous.Center.Y, room.Center.Y) - 3;
		int bottom = Math.Max(previous.Center.Y, room.Center.Y) + 4;
		Rectangle connector = new(connectorLeft, top, connectorRight - connectorLeft,
			bottom - top);
		plan = new(room, connector, default);
		return IsBounded(plan) && GenVars.structures.CanPlace(room, allowed, 12) &&
			GenVars.structures.CanPlace(connector, allowed, 0);
	}

	private static bool IsBounded(ContinentalDungeonPlan plan)
	{
		Rectangle area = plan.ProtectedArea;
		return area.Left >= 30 && area.Right < Main.maxTilesX - 30 && area.Top >= 50 &&
			area.Bottom < NativeUnderworldSystem.Layer - 100 && plan.Connector.Width > 0 &&
			plan.Connector.Height > 0;
	}

	private static bool OverlapsAccepted(Rectangle area,
		IReadOnlyList<ContinentalDungeonBranchPlan> accepted, int padding)
	{
		Rectangle padded = new(area.X - padding, area.Y - padding,
			area.Width + padding * 2, area.Height + padding * 2);
		foreach (ContinentalDungeonBranchPlan other in accepted)
			if (padded.Intersects(other.ProtectedArea))
				return true;
		return false;
	}
}
