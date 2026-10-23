using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework;

namespace DingDongBiomeHomes;
/// <summary>
/// Technical deployment contracts for the 61 distinct candidate assignments.
/// This is deliberately separate from the approved runtime manifest: none of
/// these profiles authorizes worldgen before player visual acceptance.
/// </summary>
public static partial class AuthoredCandidateDeploymentManifest
{
	private static readonly Dictionary<string, AuthoredDeploymentProfile> ByNpc = BuildProfiles();
	private static readonly ReadOnlyCollection<AuthoredDeploymentProfile> Profiles = new(new List<AuthoredDeploymentProfile>(ByNpc.Values));
	public static IReadOnlyList<AuthoredDeploymentProfile> All => Profiles;
	public static int Count => Profiles.Count;
	public static bool IsComplete => Count == NpcTownManifest.AuthoredCount;
}
