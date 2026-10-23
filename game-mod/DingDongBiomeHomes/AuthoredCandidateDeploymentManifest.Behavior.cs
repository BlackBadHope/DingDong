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
	public static bool TryGet(string npcKey, out AuthoredDeploymentProfile profile) => ByNpc.TryGetValue(npcKey, out profile);
}
