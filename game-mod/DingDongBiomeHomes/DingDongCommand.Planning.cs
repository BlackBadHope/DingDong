using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
public sealed partial class DingDongCommand
{
	private static bool TryResolveSettlementReviewId(CommandCaller caller, string selector, out int reviewId, out string failure)
	{
		reviewId = 1;
		failure = string.Empty;
		if (string.IsNullOrWhiteSpace(selector))
			return true;
		if (int.TryParse(selector, out int requestedId))
		{
			if (requestedId < 1 || requestedId > NpcTownManifest.AuthoredCount)
			{
				failure = $"review-id-out-of-range:{requestedId}";
				return false;
			}

			reviewId = requestedId;
			return true;
		}

		bool next = string.Equals(selector, "next", StringComparison.OrdinalIgnoreCase);
		bool previous = string.Equals(selector, "prev", StringComparison.OrdinalIgnoreCase);
		if (!next && !previous)
		{
			failure = $"unknown-review-selector:{selector.Replace('|', '/')}";
			return false;
		}

		if (Main.dedServ || Main.netMode != NetmodeID.SinglePlayer || caller.Player == null || !caller.Player.active)
		{
			failure = "relative-review-selector-requires-active-single-player";
			return false;
		}

		Vector2 playerTile = caller.Player.Center / 16f;
		SettlementResidenceReport nearest = null;
		float nearestDistance = float.MaxValue;
		foreach (SettlementResidenceReport residence in SettlementWorldState.Residences)
		{
			float distance = Vector2.DistanceSquared(playerTile, residence.Area.Center.ToVector2());
			if (distance >= nearestDistance)
				continue;
			nearestDistance = distance;
			nearest = residence;
		}

		if (nearest == null)
		{
			failure = "nearest-residence-not-found";
			return false;
		}

		reviewId = next ? nearest.ReviewId % NpcTownManifest.AuthoredCount + 1 : (nearest.ReviewId + NpcTownManifest.AuthoredCount - 2) % NpcTownManifest.AuthoredCount + 1;
		return true;
	}

	private static bool TryResolveReviewId(string npcKey, string presetId, out int reviewId, out string failure)
	{
		reviewId = 0;
		failure = string.Empty;
		if (!NpcTownManifest.TryGet(npcKey, out NpcTownIdentity identity))
		{
			failure = "preview-npc-not-in-canonical-ledger";
			return false;
		}

		if (!NpcAuthoredPresetManifest.TryGetCandidate(npcKey, out AuthoredPreset candidate) || !string.Equals(candidate.Id, presetId, StringComparison.Ordinal))
		{
			failure = "preview-preset-does-not-match-canonical-candidate";
			return false;
		}

		reviewId = identity.Variant + 1;
		if (reviewId < 1 || reviewId > NpcTownManifest.AuthoredCount)
		{
			failure = "canonical-review-id-out-of-range";
			reviewId = 0;
			return false;
		}

		return true;
	}
}
