using System;
using System.Collections.Generic;
using Terraria;

namespace DingDongBiomeHomes.Worldgen;

internal static class ContinentalDungeonExpansionPlanner
{
	private const int PortalVerticalSpacing = 96;

	internal static bool TryPlan(out List<ContinentalDungeonBranchPlan> branches,
		out int targetRooms, out int attempts, out double areaDeficit, out string failure)
	{
		branches = [];
		targetRooms = ContinentalDungeonExpansionPolicy.TargetRooms(out areaDeficit);
		int portalTarget = ContinentalDungeonExpansionPolicy.PortalCount(targetRooms);
		int attemptBudget = ContinentalDungeonExpansionPolicy.AttemptBudget(targetRooms);
		attempts = 0;
		failure = string.Empty;
		int top = Math.Max((int)Main.worldSurface + 100, Main.dungeonY + 140);
		int bottom = Math.Min(NativeUnderworldSystem.Layer - 300,
			top + ContinentalDungeonSurvey.ScanDepth - 200);
		if (bottom <= top)
		{
			failure = $"empty-envelope:{top}:{bottom}";
			return false;
		}
		bool[] allowed = ContinentalPlacementMaskPolicy.CreateNaturalTerrain();
		bool[] portalAllowed =
			ContinentalPlacementMaskPolicy.CreateDungeonPortalTerrain();
		var anchors = new List<ContinentalDungeonEdge>(portalTarget);
		int baseRooms = targetRooms / portalTarget;
		int extraRooms = targetRooms % portalTarget;
		if (baseRooms + (extraRooms > 0 ? 1 : 0) >
			ContinentalDungeonExpansionPolicy.MaximumRoomsPerBranch)
		{
			failure = $"branch-room-bound:{baseRooms}+{extraRooms}/{portalTarget}";
			return false;
		}
		int attemptsPerBranch = attemptBudget / portalTarget;
		for (int branchIndex = 0; branchIndex < portalTarget; branchIndex++)
		{
			int rooms = baseRooms + (branchIndex < extraRooms ? 1 : 0);
			bool found = false;
			for (int local = 0; local < attemptsPerBranch && !found; local++)
			{
				attempts++;
				int y = WorldGen.genRand.Next(top, bottom);
				if (!ContinentalDungeonSurvey.TryFindEdges(y, out ContinentalDungeonEdge left,
					out ContinentalDungeonEdge right))
					continue;
				int first = WorldGen.genRand.Next(2) == 0 ? -1 : 1;
				found = TryDirection(left, right, first, rooms, allowed, portalAllowed,
					anchors, branches) || TryDirection(left, right, -first, rooms, allowed,
					portalAllowed, anchors, branches);
			}
			if (!found)
			{
				failure = $"branch-{branchIndex + 1}-not-planned;attempts={attempts}";
				return false;
			}
		}
		int plannedRooms = 0;
		foreach (ContinentalDungeonBranchPlan branch in branches)
			plannedRooms += branch.Rooms.Count;
		if (plannedRooms != targetRooms || branches.Count <
			ContinentalDungeonExpansionPolicy.MinimumPortals || branches.Count >
			ContinentalDungeonExpansionPolicy.MaximumPortals)
		{
			failure = $"incomplete-plan:{plannedRooms}/{targetRooms};portals={branches.Count}";
			return false;
		}
		return true;
	}

	private static bool TryDirection(ContinentalDungeonEdge left,
		ContinentalDungeonEdge right, int direction, int roomCount, bool[] allowed,
		bool[] portalAllowed, List<ContinentalDungeonEdge> anchors,
		List<ContinentalDungeonBranchPlan> accepted)
	{
		ContinentalDungeonEdge candidate = direction < 0 ? left : right;
		if (!candidate.IsValid || !AnchorAvailable(anchors, candidate))
			return false;
		if (!ContinentalDungeonBranchPlanner.TryPlan(left, right, direction, roomCount,
			allowed, portalAllowed, accepted, out ContinentalDungeonBranchPlan branch))
			return false;
		accepted.Add(branch);
		anchors.Add(branch.Anchor);
		return true;
	}

	private static bool AnchorAvailable(List<ContinentalDungeonEdge> anchors,
		ContinentalDungeonEdge candidate)
	{
		foreach (ContinentalDungeonEdge anchor in anchors)
			if (Math.Abs(anchor.Y - candidate.Y) < PortalVerticalSpacing)
				return false;
		return true;
	}
}
