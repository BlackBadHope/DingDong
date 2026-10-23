using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static partial class WorldCompilerDecorationRasterizer
{
	private static (int Chests, int Crystals) PlaceRewards(WorldCompilerPlan plan)
	{
		int chests = 0;
		int crystals = 0;
		int relocations = 0;
		HashSet<long> chestTiles = CaptureChestTiles();
		var protectedAreas = WorldCompilerProtectedGeometry.Create(plan.Surface, plan.Structures);
		foreach (WorldCompilerDecoration entry in plan.Decorations.Entries)
		{
			if (!IsReward(entry.Kind)) continue;
			if (entry.Kind == WorldCompilerDecorationKind.LifeCrystal)
			{
				if (!TryPlaceLifeCrystal(plan, entry, chestTiles, protectedAreas,
					out bool relocated))
					throw RewardFailure("life-crystal", entry);
				if (relocated) relocations++;
				crystals++;
				continue;
			}
			if (!TryPlaceChest(plan, entry, chestTiles, protectedAreas, out int index,
				out bool moved))
				throw RewardFailure("chest", entry);
			if (moved) relocations++;
			FillChest(Main.chest[index], entry, plan.Seed);
			chests++;
		}
		if (relocations > 0)
			global::DingDongBiomeHomes.DingDongBiomeHomes.Instance.Logger.Info(
				$"DINGDONG_WORLD_COMPILER_REWARDS|verdict=PASS" +
				$"|chests={chests}|crystals={crystals}|relocated={relocations}");
		return (chests, crystals);
	}

	private static void FillChest(Chest chest, WorldCompilerDecoration entry,
		ulong seed)
	{
		int[] surface = [ItemID.HermesBoots, ItemID.CloudinaBottle,
			ItemID.BandofRegeneration, ItemID.MagicMirror];
		int[] underground = [ItemID.Extractinator, ItemID.ShoeSpikes,
			ItemID.LavaCharm, ItemID.MagicMirror];
		int[] pool = entry.Kind == WorldCompilerDecorationKind.SurfaceChest ?
			surface : underground;
		int primary = pool[WorldCompilerSeed.Range(seed, entry.X, 701, 0, pool.Length)];
		chest.item[0].SetDefaults(primary);
		chest.item[1].SetDefaults(ItemID.RecallPotion);
		chest.item[1].stack = WorldCompilerSeed.Range(seed, entry.X, 703, 2, 7);
		chest.item[2].SetDefaults(ItemID.Torch);
		chest.item[2].stack = WorldCompilerSeed.Range(seed, entry.X, 705, 12, 35);
		chest.item[3].SetDefaults(ItemID.GoldCoin);
		chest.item[3].stack = WorldCompilerSeed.Range(seed, entry.X, 707, 1, 5);
	}
}
