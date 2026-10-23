using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
public static partial class CandidateDeploymentSitePlanner
{
	private static int CountPersistentObjects(Rectangle protectedArea, Rectangle assetArea, int blendMargin, AuthoredSceneMutationMask mutationMask, out IReadOnlyList<PersistentObjectCollision> collisions)
	{
		Dictionary<Point16, HashSet<string>> anchors = new();
		foreach (KeyValuePair<Point16, TileEntity> entry in TileEntity.ByPosition)
		{
			Point16 position = entry.Key;
			if (protectedArea.Contains(position.X, position.Y) && MutationMayTouchAnchor(position.X, position.Y, assetArea, blendMargin, mutationMask))
				RecordCollision(anchors, position, $"TileEntity:{entry.Value.GetType().Name}");
		}

		foreach (Chest chest in Main.chest)
		{
			if (chest != null && protectedArea.Contains(chest.x, chest.y) && MutationMayTouchAnchor(chest.x, chest.y, assetArea, blendMargin, mutationMask))
				RecordCollision(anchors, new Point16(chest.x, chest.y), "Chest");
		}

		foreach (Sign sign in Main.sign)
		{
			if (sign != null && protectedArea.Contains(sign.x, sign.y) && MutationMayTouchAnchor(sign.x, sign.y, assetArea, blendMargin, mutationMask))
				RecordCollision(anchors, new Point16(sign.x, sign.y), "Sign");
		}

		List<PersistentObjectCollision> details = new();
		foreach (KeyValuePair<Point16, HashSet<string>> entry in anchors)
		{
			List<string> kinds = new(entry.Value);
			kinds.Sort(StringComparer.Ordinal);
			details.Add(new PersistentObjectCollision(entry.Key, string.Join('+', kinds)));
		}

		details.Sort(static (left, right) =>
		{
			int x = left.Position.X.CompareTo(right.Position.X);
			return x != 0 ? x : left.Position.Y.CompareTo(right.Position.Y);
		});
		collisions = details;
		return anchors.Count;
	}

	private static void RecordCollision(Dictionary<Point16, HashSet<string>> anchors, Point16 position, string kind)
	{
		if (!anchors.TryGetValue(position, out HashSet<string> kinds))
		{
			kinds = new HashSet<string>(StringComparer.Ordinal);
			anchors.Add(position, kinds);
		}

		kinds.Add(kind);
	}

	private static bool MutationMayTouchAnchor(int anchorX, int anchorY, Rectangle assetArea, int blendMargin, AuthoredSceneMutationMask mutationMask)
	{
		const int conservativeObjectRadius = 4;
		// BlendMargin is a search/separation envelope, not a write operation.  The
		// importer writes only exact mutation-mask cells (plus local framing), so a
		// chest merely sitting in that wider envelope must remain foreign-owned.
		_ = blendMargin;
		for (int worldX = anchorX - conservativeObjectRadius; worldX <= anchorX + conservativeObjectRadius; worldX++)
		{
			for (int worldY = anchorY - conservativeObjectRadius; worldY <= anchorY + conservativeObjectRadius; worldY++)
			{
				if (assetArea.Contains(worldX, worldY))
				{
					if (mutationMask.MutatesLocal(worldX - assetArea.X, worldY - assetArea.Y))
						return true;
					continue;
				}
			}
		}

		return false;
	}
}
