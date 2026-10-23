using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
public sealed record CandidateDeploymentSite(string NpcKey, string PresetId, HomeBiome Biome, Rectangle AssetArea, Rectangle ProtectedArea, Point TargetReference, string Fallback, IReadOnlyList<PersistentObjectCollision> PersistentCollisions, ResidenceAltitudeBand AltitudeBand, int SurfaceRelief)
{
	public int PersistentObjectOverlaps => PersistentCollisions.Count;
	public string PersistentCollisionSummary => PersistentCollisions.Count == 0 ? "none" : string.Join(';', PersistentCollisions);
}
