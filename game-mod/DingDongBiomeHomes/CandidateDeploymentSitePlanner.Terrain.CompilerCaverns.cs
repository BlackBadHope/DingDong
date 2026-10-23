using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using DingDongBiomeHomes.Worldgen.Compiler;
using DingDongBiomeHomes.Worldgen.Compiler.Runtime;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class CavernPlanningSearch
	{
		private bool TryFindCompilerCavern(out CandidateDeploymentSite site,
			out bool compilerDomain, out string failure)
		{
			site = null;
			failure = string.Empty;
			WorldCompilerPlan plan = WorldCompilerRuntimeState.Current;
			compilerDomain = plan != null;
			if (!compilerDomain) return false;

			int rooms = 0, horizontal = 0, altitude = 0, organic = 0, eligible = 0;
			foreach (WorldCompilerRoom room in plan.Structures.Rooms)
			{
				if (room.Kind is not (WorldCompilerStructureKind.ResidenceCavern or
					WorldCompilerStructureKind.MushroomResidenceCavern)) continue;
				rooms++;
				if (TryCollectRoom(room, ref horizontal, ref altitude,
					ref organic, ref eligible, out site)) return true;
			}

			if (bestCleanSite != null)
			{
				site = bestCleanSite;
				return true;
			}
			if (allowPersistentAdvisory && bestAdvisorySite != null)
			{
				site = bestAdvisorySite;
				return true;
			}
			failure = $"compiler-cavern-domain-empty:rooms={rooms}" +
				$"|horizontal={horizontal}|altitude={altitude}" +
				$"|organic={organic}|eligible={eligible}";
			return false;
		}

		private bool TryCollectRoom(WorldCompilerRoom room, ref int horizontal,
			ref int altitude, ref int organic, ref int eligible,
			out CandidateDeploymentSite immediate)
		{
			immediate = null;
			Rectangle mutation = mutationMask.MutationBounds;
			int margin = WorldCompilerResidenceCavernContract.InnerMargin;
			int minLeft = room.Bounds.X + margin - mutation.X + profile.ProtectedPadding;
			int maxLeft = room.Bounds.Right - margin - mutation.Right - profile.ProtectedPadding;
			if (minLeft > maxLeft) return false;
			for (int ordinal = 0; ordinal < 7; ordinal++)
			{
				int left = minLeft + (maxLeft - minLeft) * ordinal / 6;
				if (!InHorizontalDomain(left)) continue;
				horizontal++;
				int top = ResolveCompilerCavernTop(room, mutation, margin);
				if (!TryAltitude(top, out ResidenceAltitudeBand band)) continue;
				altitude++;
				Rectangle area = new(left, top, preset.Width, preset.Height);
				Rectangle protectedArea = mutationMask.ProtectedFootprint(
					area.Location, profile.ProtectedPadding);
				if (protectedArea.Top < room.Bounds.Y + margin ||
					protectedArea.Bottom > room.Bounds.Bottom - margin) continue;
				bool mushroom = profile.TargetAnchor ==
					DeploymentTargetAnchor.OrganicMushroomCavernEnvelope;
				if (!LooksLikeOrganicCavern(protectedArea, mushroom) ||
					!HasCompatibleTerrainAlongExteriorContour(area, mutationMask, profile.TargetBiome) ||
					requireReviewedGroundSupport && !HasReviewedCavernGroundSupport(
						area, profile.SourceGroundOffset.Value) ||
					ContainsWorldCriticalAnchor(protectedArea)) continue;
				organic++;
				if (IntersectsPlannedVisualFocus(area, mutationMask, plannedSites,
					allMasks, contract.BroadLayer)) continue;
				int overlaps = CountPersistentObjects(protectedArea, area,
					profile.BlendMargin, mutationMask,
					out IReadOnlyList<PersistentObjectCollision> collisions);
				var candidate = new CandidateDeploymentSite(profile.NpcKey,
					profile.PresetId, profile.TargetBiome, area, protectedArea,
					new Point(area.Center.X, area.Center.Y), "CompilerCavern",
					collisions, band, -1);
				eligible++;
				if (TryRank(candidate, overlaps, out immediate)) return true;
			}
			return false;
		}

		private int ResolveCompilerCavernTop(WorldCompilerRoom room,
			Rectangle mutation, int margin)
		{
			if (requireReviewedGroundSupport)
			{
				int floorY = room.Bounds.Bottom -
					WorldCompilerResidenceCavernContract.FloorDepth;
				return floorY - (preset.Height - 1 -
					profile.SourceGroundOffset!.Value);
			}

			int protectedBottom = room.Bounds.Bottom - margin;
			return protectedBottom - mutation.Bottom - profile.ProtectedPadding;
		}

		private bool InHorizontalDomain(int left)
		{
			foreach ((int min, int max) in horizontalRanges)
				if (left >= min && left <= max) return true;
			return false;
		}

		private bool TryAltitude(int top, out ResidenceAltitudeBand band)
		{
			foreach ((ResidenceAltitudeBand candidate, int min, int max) in altitudeRanges)
				if (top >= min && top <= max)
				{
					band = candidate;
					return true;
				}
			band = default;
			return false;
		}
	}
}
