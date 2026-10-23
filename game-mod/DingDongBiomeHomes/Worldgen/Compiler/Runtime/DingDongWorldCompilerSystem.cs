using Terraria;
using Terraria.ModLoader;
using DingDongBiomeHomes.Worldgen.Compiler.Api;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

public sealed partial class DingDongWorldCompilerSystem : ModSystem
{
	public override void PreWorldGen()
	{
		WorldCompilerRuntimeState.Reset();
		if (!WorldCompilerActivation.IsActive)
			return;
		StartJournal();
		journal.Run("DingDong Compiler: Plan", () => {
		WorldCompilerExtensionRegistry.BeginWorld();
		int numericSeed = Main.ActiveWorldFileData?.Seed ?? 0;
		WorldCompilerPlan plan = WorldCompilerRuntimeState.Prepare(Main.maxTilesX,
			Main.maxTilesY, unchecked((ulong)(uint)numericSeed));
		Mod.Logger.Info("DINGDONG_WORLD_COMPILER_PLAN|verdict=PASS" +
			$"|world={plan.Width}x{plan.Height}|bands={plan.Bands.Length}" +
			$"|chunks={plan.Chunks.Length}|phases={plan.Phases.Length}" +
			$"|fingerprint={plan.Fingerprint:X16}|mutated=0");
		});
	}

	public override void OnWorldUnload()
	{
		WorldCompilerRuntimeState.Reset();
		WorldCompilerExtensionRegistry.EndWorld();
	}

	public override void Unload()
	{
		WorldCompilerRuntimeState.Reset();
		WorldCompilerExtensionRegistry.EndWorld();
	}
}
