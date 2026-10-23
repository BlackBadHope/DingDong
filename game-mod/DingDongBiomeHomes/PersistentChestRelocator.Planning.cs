using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;

namespace DingDongBiomeHomes;
/// <summary>
/// Lossless chest relocation for dedicated probes and authored worldgen. The
/// existing chest index is rebound to a verified destination before source
/// tiles are cleared, so full modded chest arrays remain safe.
/// </summary>
public static partial class PersistentChestRelocator
{
	public static bool TryPlan(CandidateDeploymentSite site, out ChestRelocationPlan plan, out string failure) => TryPlan(site, Array.Empty<Rectangle>(), out plan, out failure);
	public static bool TryPlan(CandidateDeploymentSite site, IReadOnlyList<Rectangle> forbiddenAreas, out ChestRelocationPlan plan, out string failure)
	{
		plan = null;
		if (site.PersistentCollisions.Count != 1)
		{
			failure = $"expected-one-collision:{site.PersistentCollisions.Count}";
			return false;
		}

		return TryPlanCollision(site.ProtectedArea, site.PersistentCollisions[0], forbiddenAreas, out plan, out failure);
	}

	public static bool TryPlanAll(IReadOnlyList<CandidateDeploymentSite> sites, out List<ChestRelocationPlan> plans, out string failure)
	{
		plans = new List<ChestRelocationPlan>();
		List<Rectangle> forbiddenAreas = new();
		List<Rectangle> foreignAreas = new();
		// Relocation is part of our write transaction: a safe house does not
		// authorize clearing a source from, or depositing its displaced chest
		// inside, a foreign structure.
		if (!Worldgen.Compiler.Runtime.WorldCompilerActivation.IsActive)
		{
			foreignAreas.AddRange(Worldgen.ForeignStructureIntegrityGuard.ReadReservations());
			forbiddenAreas.AddRange(foreignAreas);
		}
		foreach (CandidateDeploymentSite site in sites)
			forbiddenAreas.Add(site.ProtectedArea);
		HashSet<Point16> sources = new();
		foreach (CandidateDeploymentSite site in sites)
		{
			if (site.PersistentCollisions.Count > 4)
			{
				failure = $"too-many-collisions:{site.NpcKey}/{site.PresetId}:{site.PersistentCollisions.Count}";
				return false;
			}

			foreach (PersistentObjectCollision collision in site.PersistentCollisions)
			{
				Rectangle sourceArea = new(collision.Position.X, collision.Position.Y, 2, 2);
				foreach (Rectangle foreign in foreignAreas)
				{
					if (!sourceArea.Intersects(foreign)) continue;
					failure = $"foreign-protected-source:{collision.Position.X},{collision.Position.Y}";
					return false;
				}

				if (!sources.Add(collision.Position))
				{
					// SAT pair eval allows overlapping protected padding when mutation
					// masks do not intersect, so two sites can both record the same
					// chest. Relocating that chest once (already planned, destination
					// already outside every protected area) is lossless world-safety.
					// A second plan would try to move the same record twice. Non-chest
					// duplicates stay fail-closed.
					if (string.Equals(collision.Kind, "Chest", StringComparison.Ordinal))
						continue;
					failure = $"duplicate-source-collision:{collision.Position.X},{collision.Position.Y}";
					return false;
				}

				if (!TryPlanCollision(site.ProtectedArea, collision, forbiddenAreas, out ChestRelocationPlan plan, out string collisionFailure))
				{
					failure = $"{site.NpcKey}/{site.PresetId}:{collisionFailure}";
					return false;
				}

				plans.Add(plan);
				forbiddenAreas.Add(new Rectangle(plan.DestinationTopLeft.X - ObjectClearance, plan.DestinationTopLeft.Y - ObjectClearance, 2 + ObjectClearance * 2, 2 + ObjectClearance * 2));
			}
		}

		failure = string.Empty;
		return true;
	}
}
