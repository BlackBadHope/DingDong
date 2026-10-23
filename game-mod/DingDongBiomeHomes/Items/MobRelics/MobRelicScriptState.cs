using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using DingDongBiomeHomes.Items.SynergyArmor;

namespace DingDongBiomeHomes.Items.MobRelics;
internal sealed class MobRelicScriptState
{
	internal int Cooldown;
	internal int ActiveTicks;
	internal int TwistTicks;
	internal int HitCount;
	internal int SameTargetHits;
	internal int LastTarget = -1;
	internal int LastDamageMask;
}
