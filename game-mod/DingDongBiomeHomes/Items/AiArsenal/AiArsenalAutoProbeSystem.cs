using System;
using System.Collections.Generic;
using System.Linq;
using DingDongBiomeHomes;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.AiArsenal;
public sealed class AiArsenalAutoProbeSystem : ModSystem
{
	private const string EnabledEnvironment = "DINGDONG_AI_ARSENAL_E2E_AUTO";
	public override void PostAddRecipes()
	{
		if (!Main.dedServ || !string.Equals(Environment.GetEnvironmentVariable(EnabledEnvironment), "1", StringComparison.Ordinal))
		{
			return;
		}

		AiArsenalAudit.Run(Mod, requireRecipes: true, exerciseDamage: true);
	}
}
