using DingDongBiomeHomes.Worldgen.Compiler.Api;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

public sealed partial class DingDongWorldCompilerSystem
{
	private void RunCompatibilityExtensions(GenerationProgress progress,
		GameConfiguration _)
	{
		progress.Message = "Running registered compatibility extensions";
		RunExtensions(WorldCompilerExtensionPhase.AfterCompatibility);
	}

	private void RunExtensions(WorldCompilerExtensionPhase phase)
	{
		if (!WorldCompilerExtensionRegistry.HasActions(phase)) return;
		int count = WorldCompilerExtensionRegistry.Execute(phase);
		Receipt.ExtensionActions += count;
		Mod.Logger.Info("DINGDONG_WORLD_COMPILER_EXTENSION|verdict=PASS" +
			$"|phase={phase}|actions={count}");
	}
}
