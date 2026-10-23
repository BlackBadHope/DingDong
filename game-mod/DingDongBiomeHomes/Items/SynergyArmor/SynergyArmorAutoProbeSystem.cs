using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DingDongBiomeHomes.Items.MobRelics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.SynergyArmor;
public sealed class SynergyArmorAutoProbeSystem : ModSystem
{
	private const string EnabledEnvironment = "DINGDONG_SYNERGY_ARMOR_E2E_AUTO";
	public override void PostAddRecipes()
	{
		if (Main.dedServ && string.Equals(Environment.GetEnvironmentVariable(EnabledEnvironment), "1", StringComparison.Ordinal))
			SynergyArmorAudit.Run(Mod, requireRecipes: true, exerciseGameplay: true);
	}
}
