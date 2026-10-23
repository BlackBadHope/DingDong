using System;
using Terraria;
using Terraria.ID;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static partial class WorldCompilerProgressionObjects
{
	internal static void ValidateAnchor(WorldCompilerProgressionReceipt receipt, string phase)
	{
		foreach (WorldCompilerProgressionObject entry in receipt.Objects)
		{
			if (entry.Name != "golem-altar") continue;
			if (!WorldCompilerAltarContract.Valid(entry.Bounds, GenVars.lAltarX, GenVars.lAltarY,
				(x, y) => Main.tile[x, y].HasTile && Main.tile[x, y].TileType == TileID.LihzahrdAltar))
				throw new InvalidOperationException($"Actual altar anchor invalid {phase}: {GenVars.lAltarX},{GenVars.lAltarY}.");
			global::DingDongBiomeHomes.DingDongBiomeHomes.Instance.Logger.Info(
				$"DINGDONG_WORLD_COMPILER_ALTAR|verdict=PASS|phase={phase}|at={GenVars.lAltarX},{GenVars.lAltarY}|tileRead=True");
			return;
		}
		throw new InvalidOperationException($"Actual Golem altar missing {phase}.");
	}
}
