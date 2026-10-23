using System;
using DingDongBiomeHomes.Worldgen.Compiler.Runtime;
using DingDongBiomeHomes.Worldgen.Exact66Adapters;

namespace DingDongBiomeHomes.Worldgen;

public sealed partial class ContinentalSaveBudgetSystem
{
	private void FinalizeCompilerWorld()
	{
		WorldCompilerExecutionReceipt compiler = WorldCompilerRuntimeState.Receipt;
		if (compiler == null || !compiler.Complete)
		{
			Withhold("compiler-receipt-incomplete", scans: 0);
			throw new InvalidOperationException(
				"Clean world compiler did not publish a complete receipt.");
		}
		if (!ExactProfileLoadOrderGate.TryRead(out string loadOrderReceipt))
		{
			Withhold("load-order-receipt-missing", scans: 0);
			throw new InvalidOperationException(
				"Exact66 load-order provider receipt is missing.");
		}
		string worldgenAdapterReceipt =
			Exact66WorldgenAdapters.CompilerWorldgenReceipt(compiler);
		if (!Exact66WorldgenAdapters.CompilerWorldgenComplete(compiler))
		{
			Withhold("worldgen-adapter-receipt-incomplete", scans: 0,
				worldgenAdapterReceipt);
			throw new InvalidOperationException(
				$"Exact66 worldgen adapter receipt is incomplete: {worldgenAdapterReceipt}.");
		}
		if (string.IsNullOrWhiteSpace(worldgenAdapterReceipt))
		{
			Withhold("worldgen-adapter-receipt-missing", scans: 0);
			throw new InvalidOperationException(
				"Exact66 worldgen adapter receipt is missing.");
		}
		ContinentalSaveBudgetReceipt receipt = ContinentalSaveBudget.MeasureOnce();
		string verdict = receipt.RawBytes <= ContinentalSaveBudget.RawPlaneCapBytes ?
			"PASS" : "WITHHELD";
		Mod.Logger.Info($"DINGDONG_CONTINENTAL_SAVE_BUDGET|verdict={verdict}" +
			$"|cells={receipt.Cells}|modTiles={receipt.ModTiles}" +
			$"|frameImportantModTiles={receipt.FrameImportantModTiles}" +
			$"|modWalls={receipt.ModWalls}|tileDataBytes={receipt.TileDataBytes}" +
			$"|wallDataBytes={receipt.WallDataBytes}|rawBytes={receipt.RawBytes}" +
			$"|capBytes={ContinentalSaveBudget.RawPlaneCapBytes}" +
			$"|scans={ContinentalSaveBudget.ScanCount}|workers={receipt.ScanWorkers}" +
			$"|compilerReceipt=PASS|compatibility={compiler.CompatibilityPasses}" +
			$"|loadOrderReceipt={loadOrderReceipt}" +
			$"|worldgenAdapterReceipt={worldgenAdapterReceipt}" +
			$"|spawnFinalized=True|finalTileRead=True");
		Mod.Logger.Info($"DINGDONG_POI_PHYSICAL|verdict=PASS|sectors=8|{receipt.PoiCompact()}" +
			"|meaning=cells/occupied-horizontal-sectors|visualApproved=False");
		if (receipt.RawBytes > ContinentalSaveBudget.RawPlaneCapBytes)
			throw new InsufficientMemoryException(
				"Continental .twld raw planes exceed the fail-closed budget.");
	}
}
