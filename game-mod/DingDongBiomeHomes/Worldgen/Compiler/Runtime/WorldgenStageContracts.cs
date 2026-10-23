using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static class WorldgenStageContracts
{
	internal static void Append(List<StageObservation> result, WorldCompilerProgressionReceipt receipt)
	{
		foreach (var rule in new[] { ("golem-altar", 1), ("temple-locked-door", 1),
			("queen-bee-larva", 6), ("evil-altar", 8), ("evil-orb", 8), ("hellforge", 12) })
		{
			int actual = receipt.Objects.Count(o => o.Name == rule.Item1);
			result.Add(new("obligation:object:" + rule.Item1, actual == rule.Item2, $"registered={actual};expected={rule.Item2}"));
		}
		foreach (var rule in new[] { ("dungeon", 8), ("jungle", 4), ("sky", 4),
			("underworld", 5), ("pyramid", 3), ("temple", 1) })
		{
			if (rule.Item1 == "pyramid" && receipt.PyramidLootPending)
			{
				result.Add(new("pending:chest:pyramid", true, "scheduled=after-CursedCoffinArena;finalMinimum=3;notYetValidated=True"));
				continue;
			}
			int actual = receipt.Chests.Where(c => c.Kind == rule.Item1).Select(c => c.Primary).Distinct().Count();
			result.Add(new("obligation:chest:" + rule.Item1, actual >= rule.Item2, $"registeredVariety={actual};minimum={rule.Item2}"));
		}
		Check(result, "anchor:golem", () => {
			var altar = receipt.Objects.FirstOrDefault(o => o.Name == "golem-altar");
			bool valid = altar.Name != null && WorldCompilerAltarContract.Valid(altar.Bounds, GenVars.lAltarX, GenVars.lAltarY,
				(x, y) => Main.tile[x, y].HasTile && Main.tile[x, y].TileType == TileID.LihzahrdAltar);
			return new("anchor:golem", valid, $"at={GenVars.lAltarX},{GenVars.lAltarY}");
		});
		Check(result, "paths:reader", () => {
			WorldgenStagePaths.Append(result);
			return new("paths:reader", true, "all planned routes evaluated");
		});
	}

	internal static void Check(List<StageObservation> result, string key, Func<StageObservation> read)
	{
		try { result.Add(read()); }
		catch (Exception error) { result.Add(new(key, false, "checkError=" + error)); }
	}
}
