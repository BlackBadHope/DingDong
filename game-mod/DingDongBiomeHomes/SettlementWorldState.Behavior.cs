using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;
public sealed partial class SettlementWorldState
{
	internal static void BeginGeneration(string seedProfile, int plannedResidents, int housesPerDistrict)
	{
		Reset();
		SeedProfile = seedProfile;
		PlannedResidents = plannedResidents;
		HousesPerDistrict = housesPerDistrict;
	}

	internal static void AbortGeneration() => Reset();
	public static bool TryGetResidence(int reviewId, out SettlementResidenceReport report)
	{
		foreach (SettlementResidenceReport candidate in ResidenceReports)
		{
			if (candidate.ReviewId == reviewId)
			{
				report = candidate;
				return true;
			}
		}

		report = null;
		return false;
	}

	public static bool TryGetResidence(string npcKey, out SettlementResidenceReport report)
	{
		foreach (SettlementResidenceReport candidate in ResidenceReports)
		{
			if (string.Equals(candidate.NpcKey, npcKey, StringComparison.Ordinal))
			{
				report = candidate;
				return true;
			}
		}

		report = null;
		return false;
	}

	private static Rectangle Envelope(IEnumerable<Rectangle> areas)
	{
		using IEnumerator<Rectangle> enumerator = areas.GetEnumerator();
		if (!enumerator.MoveNext())
			return Rectangle.Empty;
		Rectangle envelope = enumerator.Current;
		while (enumerator.MoveNext())
			envelope = Rectangle.Union(envelope, enumerator.Current);
		return envelope;
	}
}
