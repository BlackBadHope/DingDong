using System;
using System.Collections.Generic;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

public sealed class WorldCompilerProgressionPersistence : ModSystem
{
	private static TagCompound proof;
	private static WorldCompilerProgressionReceipt pendingReceipt;
	private static IReadOnlyList<WorldCompilerWalkPath> pendingPaths;
	private static bool AcceptanceRun => !string.IsNullOrWhiteSpace(
		Environment.GetEnvironmentVariable("DINGDONG_X10_RUNTIME_RUN_ID"));

	internal static void Seal(WorldCompilerProgressionReceipt receipt,
		IReadOnlyList<WorldCompilerWalkPath> paths)
	{
		pendingReceipt = receipt;
		pendingPaths = paths;
		proof = WorldCompilerProgressionProof.Encode(receipt, paths);
	}

	public override void ClearWorld() { proof = null; pendingReceipt = null; pendingPaths = null; }
	public override void OnWorldUnload() => ClearWorld();

	public override void PostWorldGen()
	{
		if (pendingReceipt == null) return;
		// Load-order admission puts DingDong after loot-producing providers.
		// Seal after their PostWorldGen additions, not inside our earlier GenPass.
		WorldCompilerProgressionValidation.ValidateObjects(pendingReceipt);
		WorldCompilerProgressionValidation.ValidateChests(pendingReceipt);
		proof = WorldCompilerProgressionProof.Encode(pendingReceipt, pendingPaths);
		WorldCompilerProgressionProof.Validate(proof);
		pendingReceipt = null;
		pendingPaths = null;
		Mod.Logger.Info("DINGDONG_PROGRESSION_FINAL_SEAL|verdict=PASS|phase=PostWorldGen|lateLootIncluded=True|repair=False");
	}

	public override void SaveWorldData(TagCompound tag)
	{
		if (proof != null) tag["progressionProof"] = proof;
	}

	public override void LoadWorldData(TagCompound tag)
	{
		proof = tag.ContainsKey("progressionProof") ? tag.GetCompound("progressionProof") : null;
		if (!AcceptanceRun) return;
		if (proof == null)
		{
			Mod.Logger.Info("DINGDONG_WORLD_COMPILER_PROGRESSION_READBACK|verdict=UNCHECKED|reason=legacy-world-no-proof");
			return;
		}
		// Explicit acceptance runs only: gameplay is allowed to mine objects and empty chests.
		// Normal loads never repair terrain or reject a legitimately changed world.
		WorldCompilerProgressionProof.Validate(proof);
		Mod.Logger.Info("DINGDONG_WORLD_COMPILER_PROGRESSION_READBACK|verdict=PASS" +
			$"|objects={proof.GetList<TagCompound>("objects").Count}" +
			$"|chests={proof.GetList<TagCompound>("chests").Count}" +
			$"|paths={proof.GetList<TagCompound>("paths").Count}|tileRead=True|repair=False");
	}
}
