using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Generation;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes;
public sealed partial class BiomeHomeWorldgenSystem
{
	private static Dictionary<Point16, TileAnchorSnapshot> SnapshotTileEntityAnchors()
	{
		Dictionary<Point16, TileAnchorSnapshot> snapshots = new();
		foreach (Point16 position in TileEntity.ByPosition.Keys)
		{
			Tile tile = Main.tile[position.X, position.Y];
			snapshots[position] = new TileAnchorSnapshot(tile.HasTile, tile.TileType, tile.TileFrameX, tile.TileFrameY);
		}

		return snapshots;
	}

	private static string DescribeSeedProfile()
	{
		List<string> flags = new();
		if (Main.drunkWorld)
			flags.Add("drunk");
		if (Main.getGoodWorld)
			flags.Add("for-the-worthy");
		if (Main.tenthAnniversaryWorld)
			flags.Add("celebration");
		if (Main.dontStarveWorld)
			flags.Add("constant");
		if (Main.notTheBeesWorld)
			flags.Add("not-the-bees");
		if (Main.remixWorld)
			flags.Add("remix");
		if (Main.noTrapsWorld)
			flags.Add("no-traps");
		if (Main.zenithWorld)
			flags.Add("zenith");
		return flags.Count == 0 ? "ordinary" : string.Join(",", flags);
	}
}
