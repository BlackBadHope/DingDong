using Microsoft.Xna.Framework;
using DingDongBiomeHomes.Worldgen;
using DingDongBiomeHomes.Worldgen.Compiler.Runtime;

namespace DingDongBiomeHomes;

// 30.10 evening (owner: "track the house's own blocks, not the whole carried chunk"): the planner
// demanded that two residences' ProtectedArea rectangles (mutation bounds + 8-36 tile urban buffer)
// stay disjoint. The build itself (AuthoredResidenceStructureReservation.TryReserveAll) only needs
// the write envelopes (mutation bounds + ResidenceWriteSafety.Halo) to stay disjoint; overlapping
// buffers are reserved as nested protected structures without any shared write. On the owner seed
// that stricter planner rule left the surface 3-4 conflicts short on every attempt, so the world
// got no residences. Attempt 1 keeps the historic rule (worlds that already plan stay identical);
// later attempts use the build's own rule. Exact masks, visual gaps and owner rules are unchanged.
public static partial class CandidateDeploymentSitePlanner
{
	internal static bool UseWriteEnvelopeSpacing;

	// DINGDONG_PLANNER_WRITE_ENVELOPE_FROM (diagnostic): first retry index using the write rule.
	internal static readonly int WriteEnvelopeFromRetry =
		int.TryParse(System.Environment.GetEnvironmentVariable("DINGDONG_PLANNER_WRITE_ENVELOPE_FROM"), out int from) && from >= 0 ? from : 1;

	internal static bool ResidenceEnvelopesOverlap(CandidateDeploymentSite left, CandidateDeploymentSite right)
	{
		if (!UseWriteEnvelopeSpacing || WorldCompilerRuntimeState.Current != null)
			return left.ProtectedArea.Intersects(right.ProtectedArea);
		return WriteEnvelope(left).Intersects(WriteEnvelope(right));
	}

	private static Rectangle WriteEnvelope(CandidateDeploymentSite site) =>
		AuthoredCandidateDeploymentManifest.TryGet(site.NpcKey, out AuthoredDeploymentProfile profile)
			? ResidenceWriteSafety.Bounds(site.ProtectedArea, profile.ProtectedPadding) : site.ProtectedArea;
}
