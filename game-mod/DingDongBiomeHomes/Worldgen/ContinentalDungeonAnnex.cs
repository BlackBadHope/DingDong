using System;
using System.Collections.Generic;
using Terraria;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen;

internal static class ContinentalDungeonAnnex
{
	internal static ContinentalDungeonExpansionResult Place()
	{
		if (Main.dungeonX < 30 || Main.dungeonX >= Main.maxTilesX - 30)
			throw new InvalidOperationException(
				$"Dungeon entrance is invalid: {Main.dungeonX},{Main.dungeonY}; mutated=0.");
		ContinentalDungeonStyle style = ContinentalDungeonSurvey.DetectStyle();
		if (!ContinentalDungeonExpansionPlanner.TryPlan(
			out List<ContinentalDungeonBranchPlan> branches, out int targetRooms,
			out int attempts, out double areaDeficit, out string failure))
			throw new InvalidOperationException(
				$"Dungeon scenic expansion withheld: {failure}; mutated=0.");

		int rooms = 0;
		int portals = 0;
		foreach (ContinentalDungeonBranchPlan branch in branches)
			foreach (ContinentalDungeonPlan plan in branch.Rooms)
			{
				rooms++;
				if (plan.HasPortal) portals++;
			}
		if (rooms != targetRooms || portals != branches.Count || portals <
			ContinentalDungeonExpansionPolicy.MinimumPortals || portals >
			ContinentalDungeonExpansionPolicy.MaximumPortals)
			throw new InvalidOperationException(
				$"Dungeon scenic expansion receipt mismatch: rooms={rooms}/{targetRooms}; portals={portals}; branches={branches.Count}; mutated=0.");

		// All accepted branch envelopes enter StructureMap before the first tile write.
		foreach (ContinentalDungeonBranchPlan branch in branches)
			GenVars.structures.AddProtectedStructure(branch.ProtectedArea, 12);
		foreach (ContinentalDungeonBranchPlan branch in branches)
			foreach (ContinentalDungeonPlan plan in branch.Rooms)
				ContinentalDungeonAnnexBuilder.Build(plan, style);
		return new(rooms, portals, branches.Count, targetRooms, attempts, areaDeficit);
	}
}
