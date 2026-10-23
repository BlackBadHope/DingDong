using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DingDongBiomeHomes.Items.MobRelics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.SynergyArmor;
internal static partial class SynergyArmorAudit
{
	private static void EnsureProbePlayers(Player player, out SynergyArmorPlayer armor, out MobRelicPlayer relic)
	{
		try
		{
			armor = player.GetModPlayer<SynergyArmorPlayer>();
			relic = player.GetModPlayer<MobRelicPlayer>();
			return;
		}
		catch (IndexOutOfRangeException)
		{
		// Dedicated PostWorldLoad can precede normal player initialization.
		}

		MethodInfo setupPlayer = typeof(PlayerLoader).GetMethod("SetupPlayer", BindingFlags.Static | BindingFlags.NonPublic) ?? throw new MissingMethodException(typeof(PlayerLoader).FullName, "SetupPlayer");
		try
		{
			setupPlayer.Invoke(null, new object[] { player });
		}
		catch (TargetInvocationException exception)when (exception.InnerException != null)
		{
			throw new InvalidOperationException("Synergy armor probe-player initialization failed.", exception.InnerException);
		}

		armor = player.GetModPlayer<SynergyArmorPlayer>();
		relic = player.GetModPlayer<MobRelicPlayer>();
	}
}
