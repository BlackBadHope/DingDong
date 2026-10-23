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
internal static partial class AccessoryEnchantingAudit
{
	private static Player ProbePlayer()
	{
		Player player = new()
		{
			whoAmI = 0,
			active = true,
			dead = false,
			statLife = 100,
			statLifeMax2 = 100
		};
		player.statDefense = Player.DefenseStat.Default;
		player.luck = 0f;
		try
		{
			_ = player.GetModPlayer<AccessoryEnchantPlayer>();
		}
		catch (IndexOutOfRangeException)
		{
			MethodInfo setup = typeof(PlayerLoader).GetMethod("SetupPlayer", BindingFlags.Static | BindingFlags.NonPublic) ?? throw new MissingMethodException(typeof(PlayerLoader).FullName, "SetupPlayer");
			setup.Invoke(null, new object[] { player });
		}

		return player;
	}
}
