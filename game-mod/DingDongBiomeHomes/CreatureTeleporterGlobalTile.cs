using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
public sealed class CreatureTeleporterGlobalTile : GlobalTile
{
	public override void HitWire(int i, int j, int type)
	{
		if (type == TileID.Teleporter && ModContent.GetInstance<DingDongServerConfig>().EnableCreatureTeleporters)
			CreatureTeleporterSystem.RecordPulse(i, j);
	}
}
