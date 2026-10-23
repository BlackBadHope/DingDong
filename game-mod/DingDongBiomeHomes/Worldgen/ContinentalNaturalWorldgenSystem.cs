using System.Collections.Generic;
using Terraria.GameContent.Generation;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen;

public sealed partial class ContinentalNaturalWorldgenSystem : ModSystem
{
	private const double BiomeWeight = 1.2;
	private const double LandmarkWeight = 0.8;
	private const double ReceiptWeight = 0.01;
	private ContinentalSurfaceSurvey survey;

	public override void PreWorldGen()
	{
		survey = null;
		ContinentalNaturalWorldgenReceipt.Reset();
		NativeTerrariaWorldgenReceipt.Reset();
	}

	private void AttachFinalNativePipeline(List<GenPass> tasks, ref double totalWeight)
	{
		if (Compiler.Runtime.WorldCompilerActivation.IsActive) return;
		if (!ContinentalWorldDimensions.IsContinental ||
			!ModContent.GetInstance<DingDongServerConfig>().EnableContinentalWorlds)
			return;
		// Preserve the supplied Terraria/provider passes and their order. Neither
		// the schematic compiler nor late biome repaint/rectangular annexes belong
		// in normal x10 generation. Memory and bounds adapters remain independent.
		NativeTerrariaWorldgenPipeline.Attach(Mod, tasks, ref totalWeight);
	}

	private static int FindUniqueIndex(List<GenPass> tasks, string name, out int count)
	{
		int index = -1;
		count = 0;
		for (int position = 0; position < tasks.Count; position++)
			if (tasks[position].Name == name)
			{
				index = position;
				count++;
			}
		return count == 1 ? index : -1;
	}
}
