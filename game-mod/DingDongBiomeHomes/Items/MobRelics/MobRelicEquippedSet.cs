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
internal sealed class MobRelicEquippedSet
{
	private int tierMask;
	private MobRelicEffectProfile anchor;
	private bool hasAnchor;
	internal int DistinctTierCount { get; private set; }
	internal MobRelicEffectProfile Anchor => anchor;

	internal void Register(MobRelicEffectProfile profile)
	{
		int bit = 1 << (int)profile.Tier;
		if ((tierMask & bit) == 0)
		{
			tierMask |= bit;
			DistinctTierCount++;
		}

		if (!hasAnchor || profile.EchoChance > anchor.EchoChance || (Math.Abs(profile.EchoChance - anchor.EchoChance) < 0.0001f && profile.Tier > anchor.Tier))
		{
			anchor = profile;
			hasAnchor = true;
		}
	}
}
