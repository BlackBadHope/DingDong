using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.MobRelics;
internal enum MobRelicTier : byte
{
	CommonTrace,
	CommonCurio,
	ExpertSeal,
	MasterRelic,
	LegendaryMyth
}
