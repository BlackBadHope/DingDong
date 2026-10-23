using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes.Items.MobRelics;
public sealed class MobRelicAutoProbeSystem : ModSystem
{
	private const string EnabledEnvironment = "DINGDONG_MOB_RELIC_E2E_AUTO";
	public override void PostAddRecipes()
	{
		if (!Main.dedServ || !string.Equals(Environment.GetEnvironmentVariable(EnabledEnvironment), "1", StringComparison.Ordinal))
		{
			return;
		}

		MobRelicAudit.Run(Mod);
	}
}
