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
	private static ulong MixPoolDigest(ulong hash, int value)
	{
		unchecked
		{
			for (int shift = 0; shift < 32; shift += 8)
			{
				hash ^= (byte)(value >> shift);
				hash *= 1099511628211UL;
			}

			return hash;
		}
	}

	private static ulong ConstraintDomainDigest(IReadOnlyList<string> variables, IReadOnlyDictionary<string, IReadOnlyList<RankedLayoutCandidate>> domains)
	{
		ulong digest = 14695981039346656037UL;
		foreach (string variable in variables)
		{
			digest = MixPoolDigest(digest, variable);
			if (!domains.TryGetValue(variable, out IReadOnlyList<RankedLayoutCandidate> domain) || domain.Count == 0)
			{
				digest = MixPoolDigest(digest, -1);
				continue;
			}

			digest = MixPoolDigest(digest, AuthoredResidenceLayoutPolicy.SectorOf(domain[0].Site.AssetArea.Center.X));
			digest = MixPoolDigest(digest, domain.Count);
		}

		return digest;
	}

	private static ulong MixPoolDigest(ulong hash, string value)
	{
		unchecked
		{
			foreach (char character in value ?? string.Empty)
			{
				hash ^= (byte)character;
				hash *= 1099511628211UL;
				hash ^= (byte)(character >> 8);
				hash *= 1099511628211UL;
			}

			return hash;
		}
	}
}
