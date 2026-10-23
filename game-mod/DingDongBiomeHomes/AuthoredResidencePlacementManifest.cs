using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DingDongBiomeHomes;
/// <summary>
/// Spatial deployment metadata for the 61 authored residences.
/// This is deliberately separate from <see cref = "NpcTownManifest"/> lore and
/// from the approved visual runtime manifest. NPC identity biome remains an
/// allowed lore constraint, while the first allowed biome is the deployment
/// preference chosen for the authored asset.
/// </summary>
public static partial class AuthoredResidencePlacementManifest
{
	private static readonly Dictionary<string, AuthoredResidencePlacementContract> ByNpc = BuildContracts();
	private static readonly ReadOnlyCollection<AuthoredResidencePlacementContract> Contracts = new(new List<AuthoredResidencePlacementContract>(ByNpc.Values));
	public static IReadOnlyList<AuthoredResidencePlacementContract> All => Contracts;
	public static int Count => Contracts.Count;
}
