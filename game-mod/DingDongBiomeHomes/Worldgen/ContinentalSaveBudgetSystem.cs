using System;
using DingDongBiomeHomes.Worldgen.Exact66Adapters;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes.Worldgen;

public sealed partial class ContinentalSaveBudgetSystem : ModSystem
{
	private bool worldgenSavePending;

	public override void PostSetupContent() =>
		ExactProfileLoadOrderGate.AssertLast(Mod);

	public override void PreWorldGen()
	{
		ContinentalSaveBudget.Reset();
		worldgenSavePending = true;
	}

	public override void PostWorldGen()
	{
		ModContent.GetInstance<SpawnSanctuaryWorldgenSystem>()
			.FinalizeBeforeSave();
		NativePyramidProtection.Validate(Mod, "post-worldgen");
	}

	public override void SaveWorldData(TagCompound tag)
	{
		// This explicit disposable snapshot is taken before authored residences.
		// It is labelled incomplete and must not consume the final-save receipt.
		if (NativeWorldgenProbeCheckpoint.Saving)
			return;
		if (!worldgenSavePending)
			return;
		if (Compiler.Runtime.WorldCompilerActivation.IsActive)
		{
			FinalizeCompilerWorld();
			worldgenSavePending = false;
			return;
		}
		if (!ContinentalWorldDimensions.IsContinental ||
			!ModContent.GetInstance<DingDongServerConfig>().EnableContinentalWorlds)
		{
			worldgenSavePending = false;
			return;
		}
		if (!ExactProfileLoadOrderGate.TryRead(out string loadOrderReceipt))
		{
			Withhold("load-order-receipt-missing", scans: 0);
			throw new InvalidOperationException(
				"Exact65 load-order provider receipt is missing.");
		}
		string worldgenAdapterReceipt = Exact66WorldgenAdapters.WorldgenReceipt;
		if (!Exact66WorldgenAdapters.WorldgenComplete)
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
		if (!NativeTerrariaWorldgenReceipt.Complete)
		{
			Withhold("natural-receipt-missing", scans: 0);
			throw new InvalidOperationException(
				"Continental natural-worldgen receipt is missing.");
		}
		if (!NativePoiDensityReceipt.CoverageComplete)
		{
			string missing = string.Join(",", NativePoiDensityReceipt.MissingTargets());
			Withhold("poi-density-receipt-incomplete", scans: 0, missing);
			throw new InvalidOperationException(
				"Native repeatable POI density receipt is incomplete: " + missing);
		}

		ContinentalSaveBudgetReceipt receipt;
		try
		{
			receipt = ContinentalSaveBudget.MeasureOnce();
		}
		catch (Exception exception)
		{
			Mod.Logger.Error(
				$"DINGDONG_CONTINENTAL_SAVE_BUDGET|verdict=WITHHELD|reason=scan-failed|scans={ContinentalSaveBudget.ScanCount}|exception={exception.GetType().Name}");
			throw;
		}
		string verdict = receipt.RawBytes <= ContinentalSaveBudget.RawPlaneCapBytes
			? "PASS" : "WITHHELD";
		Mod.Logger.Info(
			$"DINGDONG_CONTINENTAL_SAVE_BUDGET|verdict={verdict}|cells={receipt.Cells}|modTiles={receipt.ModTiles}|frameImportantModTiles={receipt.FrameImportantModTiles}|modWalls={receipt.ModWalls}|tileDataBytes={receipt.TileDataBytes}|wallDataBytes={receipt.WallDataBytes}|rawBytes={receipt.RawBytes}|capBytes={ContinentalSaveBudget.RawPlaneCapBytes}|scans={ContinentalSaveBudget.ScanCount}|workers={receipt.ScanWorkers}|naturalReceipt=PASS|loadOrderReceipt={loadOrderReceipt}|worldgenAdapterReceipt={worldgenAdapterReceipt}|spawnFinalized=True|finalTileRead=True");
		Mod.Logger.Info($"DINGDONG_POI_PHYSICAL|verdict=PASS|sectors=8|{receipt.PoiCompact()}" +
			"|meaning=cells/occupied-horizontal-sectors|visualApproved=False");
		if (receipt.RawBytes > ContinentalSaveBudget.RawPlaneCapBytes)
			throw new InsufficientMemoryException(
				"Continental .twld raw planes exceed the fail-closed budget.");
		worldgenSavePending = false;
	}

	public override void LoadWorldData(TagCompound tag)
	{
		// A loaded world has no in-process worldgen receipt to validate. The gate is
		// armed only by PreWorldGen and is consumed by the first generated save.
		worldgenSavePending = false;
	}

	public override void Unload() => ExactProfileLoadOrderGate.Reset();

	private void Withhold(string reason, int scans, string receipt = null) =>
		Mod.Logger.Error($"DINGDONG_CONTINENTAL_SAVE_BUDGET|verdict=WITHHELD" +
			$"|reason={reason}|scans={scans}" +
			(string.IsNullOrWhiteSpace(receipt) ? "" : $"|receipt={receipt}"));
}
