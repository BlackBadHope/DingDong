using System;
using System.Collections.Generic;
using System.Linq;
using DingDongBiomeHomes.Items.AccessoryEnchanting;
using DingDongBiomeHomes.Items.MobRelics;
using DingDongBiomeHomes.Items.SynergyArmor;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;
public sealed class DingDongMultiplayerAbAutoProbeSystem : ModSystem
{
	private const string EnabledEnvironment = "DINGDONG_MULTIPLAYER_AB_E2E_AUTO";
	private const string WorldPrefix = "DingDongMultiplayerAB-";
	private bool ran;
	public override void PostWorldLoad() => RunOnce();
	public override void PostUpdateWorld() => RunOnce();
	public override void OnWorldUnload() => ran = false;
	private void RunOnce()
	{
		if (ran || !Main.dedServ || !string.Equals(Environment.GetEnvironmentVariable(EnabledEnvironment), "1", StringComparison.Ordinal))
		{
			return;
		}

		ran = true;
		string worldName = System.IO.Path.GetFileNameWithoutExtension(Main.ActiveWorldFileData?.Path ?? string.Empty);
		if (!worldName.StartsWith(WorldPrefix, StringComparison.Ordinal) || Main.player.Any(player => player.active))
		{
			Mod.Logger.Error($"DINGDONG_MULTIPLAYER_AB_E2E|verdict=FAIL|reason=guard-rejected|world={worldName}|dedicated={Main.dedServ}|syntheticClients=True|userSavesTouched=False");
			return;
		}

		DingDongMultiplayerAbProbe.Run(Mod);
	}
}
