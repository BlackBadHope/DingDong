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
internal enum MobRelicTrigger : byte
{
	AnyHit,
	Critical,
	Cadence,
	Opening,
	Finisher,
	Moving,
	Still,
	Airborne,
	Grounded,
	Wounded,
	Healthy,
	LowMana,
	HighMana,
	AfterHurt,
	CloseRange,
	LongRange,
	AlternatingClass,
	RepeatTarget,
	Daylight,
	Nightfall
}
