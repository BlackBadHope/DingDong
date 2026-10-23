using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
public sealed class DingDongGameplayContractSystem : ModSystem
{
	public override void PostSetupContent()
	{
		GameplayContractResult result = DingDongGameplayContracts.Evaluate();
		if (result.Pass)
			Mod.Logger.Info(result.Summary);
		else
			Mod.Logger.Warn(result.Summary);
		GameplayContractResult stability = DingDongGameplayContracts.EvaluateStabilityGate(result);
		if (stability.Pass)
			Mod.Logger.Info(stability.Summary);
		else
			Mod.Logger.Warn(stability.Summary);
	}
}
