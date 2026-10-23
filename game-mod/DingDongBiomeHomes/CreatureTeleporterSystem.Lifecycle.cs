using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
public sealed partial class CreatureTeleporterSystem
{
	public override void ClearWorld()
	{
		PulsePlatforms.Clear();
		CooldownByNpc.Clear();
		pulseTick = ulong.MaxValue;
		processedTick = ulong.MaxValue;
	}

	public override void PostUpdateWorld()
	{
		if (!ShouldProcessPulseContract(Main.netMode, pulseTick, Main.GameUpdateCount, processedTick, PulsePlatforms.Count))
		{
			if (Main.netMode != NetmodeID.MultiplayerClient && pulseTick == Main.GameUpdateCount && processedTick != pulseTick && PulsePlatforms.Count > 2)
			{
				processedTick = pulseTick;
				Mod.Logger.Warn($"DINGDONG_CREATURE_TELEPORT_REJECT|reason=ambiguous-pulse|platforms={PulsePlatforms.Count}");
			}

			return;
		}

		processedTick = pulseTick;
		Point16[] platforms = PulsePlatforms.OrderBy(point => point.X).ThenBy(point => point.Y).ToArray();
		List<int> fromFirst = CaptureEligibleNpcs(platforms[0]);
		List<int> fromSecond = CaptureEligibleNpcs(platforms[1]);
		foreach (int npcIndex in fromFirst)
			TryTeleport(Main.npc[npcIndex], platforms[1]);
		foreach (int npcIndex in fromSecond)
			TryTeleport(Main.npc[npcIndex], platforms[0]);
		if (CooldownByNpc.Count > 128)
		{
			foreach (int index in CooldownByNpc.Where(pair => Main.GameUpdateCount - pair.Value > TeleportCooldownTicks * 4).Select(pair => pair.Key).ToArray())
				CooldownByNpc.Remove(index);
		}
	}
}
