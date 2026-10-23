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
	private static bool ContainsOnlyRelocatableChestCollisions(CandidateDeploymentSite site, out string failure)
	{
		if (site.PersistentCollisions.Count > 4)
		{
			failure = $"too-many-collisions:{site.PersistentCollisions.Count}";
			return false;
		}

		foreach (PersistentObjectCollision collision in site.PersistentCollisions)
		{
			if (!PersistentChestRelocator.IsSupportedSourceTile(collision, out failure))
				return false;
		}

		failure = string.Empty;
		return true;
	}
}
