using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
internal static partial class EscortPathfinder
{
	internal static bool TryBuild(NPC npc, Point rawGoal, List<Point> output)
	{
		output.Clear();
		Point rawStart = npc.Bottom.ToTileCoordinates();
		Point clampedGoal = new(Math.Clamp(rawGoal.X, rawStart.X - HorizontalRadius + 4, rawStart.X + HorizontalRadius - 4), Math.Clamp(rawGoal.Y, rawStart.Y - VerticalRadius + 4, rawStart.Y + VerticalRadius - 4));
		if (!TryNearestStandable(npc, rawStart, 5, out Point start) || !TryNearestStandable(npc, clampedGoal, 9, out Point goal))
		{
			return false;
		}

		Rectangle bounds = new(Math.Max(10, rawStart.X - HorizontalRadius), Math.Max(10, rawStart.Y - VerticalRadius), HorizontalRadius * 2, VerticalRadius * 2);
		bounds.Width = Math.Min(bounds.Width, Main.maxTilesX - 10 - bounds.X);
		bounds.Height = Math.Min(bounds.Height, Main.maxTilesY - 10 - bounds.Y);
		PriorityQueue<Point, float> frontier = new();
		Dictionary<Point, Point> cameFrom = new();
		Dictionary<Point, float> costs = new();
		frontier.Enqueue(start, 0f);
		costs[start] = 0f;
		int visited = 0;
		Point reached = start;
		float reachedDistance = Heuristic(start, goal);
		while (frontier.Count > 0 && visited++ < MaximumVisitedNodes)
		{
			Point current = frontier.Dequeue();
			float distance = Heuristic(current, goal);
			if (distance < reachedDistance)
			{
				reached = current;
				reachedDistance = distance;
			}

			if (distance <= 1f)
			{
				reached = current;
				break;
			}

			foreach (Point next in Neighbors(npc, current, bounds))
			{
				float verticalPenalty = next.Y < current.Y ? (current.Y - next.Y) * 0.55f : 0f;
				float newCost = costs[current] + Vector2.Distance(current.ToVector2(), next.ToVector2()) + verticalPenalty;
				if (costs.TryGetValue(next, out float oldCost) && newCost >= oldCost)
					continue;
				costs[next] = newCost;
				cameFrom[next] = current;
				frontier.Enqueue(next, newCost + Heuristic(next, goal));
			}
		}

		if (reached == start && reachedDistance > 1f)
			return false;
		List<Point> reverse = new()
		{
			reached
		};
		while (reverse[^1] != start && cameFrom.TryGetValue(reverse[^1], out Point previous))
			reverse.Add(previous);
		reverse.Reverse();
		output.AddRange(reverse);
		return output.Count > 0;
	}
}
