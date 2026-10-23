using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.WorldBuilding;
using DingDongBiomeHomes.Worldgen;
using DingDongBiomeHomes.Worldgen.Compiler;
using DingDongBiomeHomes.Worldgen.Compiler.Runtime;

namespace DingDongBiomeHomes;

internal static class AuthoredResidenceStructureReservation
{
	internal static bool CanReserve(CandidateDeploymentSite site,
		bool[] structureOnly)
		=> CanReserve(site, structureOnly, out _);

	internal static bool CanReserve(CandidateDeploymentSite site,
		bool[] structureOnly, out string failure)
	{
		failure = "structure-map-collision";
		// ProtectedArea includes a 24-36 tile urban-spacing buffer. That buffer
		// may overlap another provider's buffer without any writes overlapping.
		// Test the complete write BOUNDING rectangle plus framing halo against
		// unchanged foreign reservations; no mutation-mask holes as a loophole.
		if (!AuthoredCandidateDeploymentManifest.TryGet(site.NpcKey, out var profile))
			return false;
		Rectangle area = WorldCompilerRuntimeState.Current == null
			? WriteEnvelope(site, profile) : site.ProtectedArea;
		if (GenVars.structures.CanPlace(area, structureOnly, 0))
		{
			if (WorldCompilerRuntimeState.Current == null &&
				NpcAuthoredPresetManifest.TryGetCandidate(site.NpcKey, out var preset) &&
				DingDongAuthoredStructureLoader.HasAmbientLavaExposure(DingDongBiomeHomes.Instance, preset, profile,
					site.AssetArea.Location, out Point threat))
			{
				failure = $"ambient-lava-exposure:{threat.X},{threat.Y}";
				return false;
			}
			failure = string.Empty;
			return true;
		}

		// Compiler residence caverns are reserved before foreign compatibility
		// passes so those passes cannot consume the authored-home envelope.  The
		// authored residence is the sole intended writer inside that exact room,
		// therefore its own reservation is an admissible nested reservation.
		WorldCompilerPlan plan = WorldCompilerRuntimeState.Current;
		if (plan == null)
			return false;
		foreach (WorldCompilerRoom room in plan.Structures.Rooms)
		{
			if (room.Kind == WorldCompilerStructureKind.JungleResidencePlot)
			{
				if (site.NpcKey == "CalValEX/Jharim" && new Rectangle(
					room.Bounds.X, room.Bounds.Y, room.Bounds.Width,
					room.Bounds.Height).Contains(area)) return true;
				continue;
			}
			if (room.Kind is not (WorldCompilerStructureKind.ResidenceCavern or
				WorldCompilerStructureKind.MushroomResidenceCavern))
				continue;
			Rectangle envelope = new(room.Bounds.X, room.Bounds.Y,
				room.Bounds.Width, room.Bounds.Height);
			if (envelope.Contains(area))
				return true;
		}
		return false;
	}

	private static Rectangle WriteEnvelope(CandidateDeploymentSite site,
		AuthoredDeploymentProfile profile) =>
		ResidenceWriteSafety.Bounds(site.ProtectedArea, profile.ProtectedPadding);

	internal static bool TryReserveAll(IReadOnlyList<CandidateDeploymentSite> sites,
		out string failure)
	{
		failure = string.Empty;
		if (sites.Count != NpcTownManifest.AuthoredCount)
		{
			failure = $"site-count:{sites.Count}/{NpcTownManifest.AuthoredCount}";
			return false;
		}
		var acceptedReservations = new List<Rectangle>(sites.Count);
		var acceptedWriteEnvelopes = new List<Rectangle>(sites.Count);
		var npcKeys = new HashSet<string>();
		bool[] structureOnly = ContinentalPlacementMaskPolicy.CreateStructureOnly();
		foreach (CandidateDeploymentSite site in sites)
		{
			Rectangle area = site.ProtectedArea;
			if (!AuthoredCandidateDeploymentManifest.TryGet(site.NpcKey, out var profile))
			{
				failure = $"missing-profile:{site.NpcKey}";
				return false;
			}
			Rectangle writeEnvelope = WorldCompilerRuntimeState.Current == null
				? WriteEnvelope(site, profile) : area;
			if (!npcKeys.Add(site.NpcKey))
			{
				failure = $"duplicate-npc:{site.NpcKey}";
				return false;
			}
			if (area.Width <= 0 || area.Height <= 0 || area.Left < 1 || area.Top < 1 ||
				area.Right >= Main.maxTilesX - 1 || area.Bottom >= Main.maxTilesY - 1)
			{
				failure = $"invalid-area:{site.NpcKey}:{area}";
				return false;
			}
			if (!CanReserve(site, structureOnly, out string admissionFailure))
			{
				failure = $"{admissionFailure}:{site.NpcKey}:{area}";
				return false;
			}
			foreach (Rectangle other in acceptedWriteEnvelopes)
				if (writeEnvelope.Intersects(other))
				{
					failure = $"planned-write-overlap:{site.NpcKey}:{writeEnvelope}:{other}";
					return false;
				}
			acceptedReservations.Add(area);
			acceptedWriteEnvelopes.Add(writeEnvelope);
		}
		// Validation is complete: reserve all 61 before chest or structure mutation.
		foreach (Rectangle area in acceptedReservations)
			GenVars.structures.AddProtectedStructure(area, 0);
		return true;
	}
}
