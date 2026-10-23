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
internal static partial class MobRelicMechanics
{
	internal static MobRelicEffectProfile CreateProfile(MobRelicSourceDefinition source, MobRelicTier tier, int mechanicOrdinal)
	{
		byte[] digest;
		ulong seed;
		MobRelicAspect aspect;
		int packedColor, crit, defense, lifeRegen, maxLife, maxMana, echoCooldown, wardDefense, wardDuration, scriptCooldown, scriptDuration;
		float damage, movement, attackSpeed, manaCost, endurance, echoChance, echoDamage, wardEndurance, scriptPower;
		MobRelicTrigger trigger;
		MobRelicInvocation invocation;
		MobRelicTwist twist;
		byte hitCadence;
		InitializeProfileSeedAndMechanics(source, tier, mechanicOrdinal, out digest, out seed, out aspect, out packedColor, out damage, out crit, out movement, out attackSpeed, out manaCost, out defense, out endurance, out lifeRegen, out maxLife, out maxMana, out echoChance, out echoDamage, out echoCooldown, out wardDefense, out wardEndurance, out wardDuration, out trigger, out invocation, out twist, out hitCadence, out scriptCooldown, out scriptDuration, out scriptPower);
		switch (tier)
		{
			case MobRelicTier.CommonTrace:
				damage = 0.025f + digest[12] % 36 / 1000f;
				crit = 1 + digest[13] % 4;
				break;
			case MobRelicTier.CommonCurio:
				movement = 0.04f + digest[12] % 41 / 1000f;
				attackSpeed = 0.02f + digest[13] % 31 / 1000f;
				manaCost = 0.01f + digest[14] % 21 / 1000f;
				break;
			case MobRelicTier.ExpertSeal:
				defense = 2 + digest[12] % 5;
				endurance = 0.01f + digest[13] % 17 / 1000f;
				lifeRegen = 1 + digest[14] % 2;
				wardDefense = 3 + digest[15] % 4;
				wardEndurance = 0.025f + digest[16] % 21 / 1000f;
				wardDuration = 150 + digest[17] % 91;
				break;
			case MobRelicTier.MasterRelic:
				damage = 0.04f + digest[12] % 36 / 1000f;
				echoChance = 0.10f + digest[13] % 71 / 1000f;
				echoDamage = 0.22f + digest[14] % 151 / 1000f;
				echoCooldown = 34 + digest[15] % 27;
				break;
			case MobRelicTier.LegendaryMyth:
				damage = 0.055f + digest[12] % 46 / 1000f;
				movement = 0.03f + digest[13] % 41 / 1000f;
				maxLife = 12 + digest[14] % 17;
				maxMana = 12 + digest[15] % 17;
				echoChance = 0.16f + digest[16] % 91 / 1000f;
				echoDamage = 0.32f + digest[17] % 181 / 1000f;
				echoCooldown = 24 + digest[18] % 25;
				wardDefense = 5 + digest[19] % 6;
				wardEndurance = 0.04f + digest[20] % 31 / 1000f;
				wardDuration = 210 + digest[21] % 121;
				break;
		}

		return new MobRelicEffectProfile(source, tier, aspect, seed, packedColor, digest[22], damage, crit, movement, attackSpeed, manaCost, defense, endurance, lifeRegen, maxLife, maxMana, echoChance, echoDamage, echoCooldown, wardDefense, wardEndurance, wardDuration, mechanicOrdinal, trigger, invocation, twist, hitCadence, scriptCooldown, scriptDuration, scriptPower);
	}
}
