using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Generation;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen;

// Owner, 27.10 (defect I): the whole underground map is sprinkled with single green moss and
// grass tiles and red/purple evil-stone specks, so the map reads as noise. Before the vanilla
// final cleanup, a single tile of moss, grass or evil/hallowed stone with at most one neighbour
// of its own family goes back to the plain host block. Moss caves and grass patches (two or more
// touching tiles) stay as they are; the surface, the Underworld and the jungle are not touched.
public sealed class ConfettiCleanupSystem : ModSystem
{
	private const double PassWeight = 25;

	public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
	{
		if (Compiler.Runtime.WorldCompilerActivation.IsActive) return;
		int final = tasks.FindIndex(t => t.Name == "Final Cleanup");
		if (final < 0) return;
		tasks.Insert(final, new PassLegacy("DingDong underground confetti cleanup", Clean, PassWeight));
		totalWeight += PassWeight;
	}

	private enum Family : byte { None, Moss, Grass, EvilStone }

	private static Family Of(Tile tile)
	{
		if (!tile.HasTile) return Family.None;
		int type = tile.TileType;
		if (TileID.Sets.Conversion.Moss[type]) return Family.Moss;
		if (type is TileID.Grass or TileID.CorruptGrass or TileID.CrimsonGrass or TileID.HallowedGrass) return Family.Grass;
		if (type is TileID.Ebonstone or TileID.Crimstone or TileID.Pearlstone) return Family.EvilStone;
		return Family.None;
	}

	private static void Clean(GenerationProgress progress, GameConfiguration configuration)
	{
		int top = (int)Main.worldSurface + 30, bottom = Main.UnderworldLayer - 20;
		long moss = 0, grass = 0, stone = 0;
		for (int x = 2; x < Main.maxTilesX - 2; x++)
		{
			if (x % 1000 == 0) progress.Set(x / (double)Main.maxTilesX);
			for (int y = top; y < bottom; y++)
			{
				Tile tile = Main.tile[x, y];
				Family family = Of(tile);
				if (family == Family.None) continue;
				int same = 0;
				for (int dx = -1; dx <= 1 && same < 2; dx++)
				for (int dy = -1; dy <= 1 && same < 2; dy++)
					if ((dx != 0 || dy != 0) && Of(Main.tile[x + dx, y + dy]) == family) same++;
				if (same >= 2) continue;
				switch (family)
				{
					case Family.Moss: tile.TileType = TileID.Stone; moss++; break;
					case Family.Grass: tile.TileType = TileID.Dirt; grass++; break;
					default: tile.TileType = TileID.Stone; stone++; break;
				}
			}
		}
		ModContent.GetInstance<DingDongBiomeHomes>().Logger.Info(
			$"DINGDONG_CONFETTI_CLEANUP|moss={moss}|grass={grass}|evilStone={stone}|rows={top}..{bottom}");
	}
}
