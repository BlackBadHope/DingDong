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
	internal static bool IsEligibleContract(bool active, bool boss, bool countedAsBoss, int realLife, int aiStyle, int width, int height, int maximumWidthTiles, int maximumHeightTiles)
	{
		return active && !boss && !countedAsBoss && realLife < 0 && aiStyle != NPCAIStyleID.Worm && width > 0 && height > 0 && width <= Math.Clamp(maximumWidthTiles, 3, 12) * 16 && height <= Math.Clamp(maximumHeightTiles, 3, 12) * 16;
	}

	internal static bool ShouldProcessPulseContract(int netMode, ulong recordedTick, ulong currentTick, ulong alreadyProcessedTick, int platformCount)
	{
		return netMode != NetmodeID.MultiplayerClient && recordedTick == currentTick && alreadyProcessedTick != recordedTick && platformCount == 2;
	}

	internal static bool TryTeleportForProbe(NPC npc, Point16 destinationPlatform) => TryTeleport(npc, destinationPlatform);
}
