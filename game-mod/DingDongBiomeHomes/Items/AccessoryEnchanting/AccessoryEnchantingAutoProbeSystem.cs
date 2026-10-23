using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes.Items.AccessoryEnchanting;
public sealed class AccessoryEnchantingAutoProbeSystem : ModSystem
{
	private const string EnabledEnvironment = "DINGDONG_ACCESSORY_ENCHANTING_E2E_AUTO";
	public override void PostAddRecipes()
	{
		if (Main.dedServ && string.Equals(Environment.GetEnvironmentVariable(EnabledEnvironment), "1", StringComparison.Ordinal))
			AccessoryEnchantingAudit.Run(Mod, exerciseGameplay: true);
	}
}
