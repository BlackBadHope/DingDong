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
internal enum MobRelicInvocation : byte
{
	ResonantBolt,
	TwinArc,
	PrismaticFan,
	ElementalBrand,
	Mend,
	ManaBloom,
	Bulwark,
	Haste,
	Fury,
	Precision,
	Leech,
	ThresholdWard,
	Shockwave,
	PhaseStep,
	Featherfall,
	Regeneration,
	Endurance,
	Fortune,
	MixedSurge,
	ClockReset
}
