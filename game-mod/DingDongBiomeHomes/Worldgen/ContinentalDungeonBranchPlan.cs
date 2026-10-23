using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace DingDongBiomeHomes.Worldgen;

internal sealed record ContinentalDungeonBranchPlan(
	IReadOnlyList<ContinentalDungeonPlan> Rooms,
	Rectangle ProtectedArea,
	ContinentalDungeonEdge Anchor,
	int Direction);
