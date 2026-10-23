using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
internal enum DamagePacketKind : byte
{
	HitIntent = 1,
	HitReceipt = 2,
	HitCommit = 3
}
