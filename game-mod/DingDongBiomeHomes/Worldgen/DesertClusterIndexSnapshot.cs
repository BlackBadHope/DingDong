using System;
using System.Collections;
using System.Reflection;
using ReLogic.Utilities;
using Microsoft.Xna.Framework;

namespace DingDongBiomeHomes.Worldgen;

internal static class DesertClusterIndexSnapshot
{
	internal static DesertClusterSpatialIndex Create(object group)
	{
		if (!Exact66Adapters.ContinentalAdapterMath.Active()) return null;
		IList clusters = (IList)group;
		var anchors = new (double X, double Y)[clusters.Count];
		FieldInfo position = null;
		for (int i = 0; i < clusters.Count; i++)
		{
			IList blocks = (IList)clusters[i];
			if (blocks.Count == 0) throw new InvalidOperationException("Empty vanilla desert cluster.");
			object block = blocks[0];
			position ??= block.GetType().GetField("Position", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
				?? throw new MissingFieldException(block.GetType().FullName, "Position");
			anchors[i] = position.GetValue(block) switch
			{
				Vector2D point => (point.X, point.Y),
				Vector2 point => (point.X, point.Y),
				_ => throw new InvalidOperationException("Unknown desert anchor representation.")
			};
		}
		return new DesertClusterSpatialIndex(anchors);
	}
}
