using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
/// <summary>
/// Candidate-envelope search shared by the independent 3F preview gate and
/// normal world generation. Independent planning intentionally ignores other
/// DingDong scenes. Simultaneous planning broad-phases with every accepted
/// protected footprint, keeps each primary authored residence focus visually
/// separate, resolves sparse-envelope overlaps against the exact mutation
/// masks, and rejects persistent-object collisions before any tile mutation.
/// </summary>
public static partial class CandidateDeploymentSitePlanner
{
	private sealed record RankedLayoutCandidate(CandidateDeploymentSite Site, int BaseScore);
	private sealed record CapacitySectorItem(string Variable, AuthoredResidencePlacementContract Contract, IReadOnlyList<CapacitySectorOption> Options);
	private readonly record struct CapacitySectorOption(int Sector, int BestScore);
}
