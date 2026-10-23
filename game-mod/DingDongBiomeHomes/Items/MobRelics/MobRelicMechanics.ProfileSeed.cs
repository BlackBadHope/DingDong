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
	private static void InitializeProfileSeedAndMechanics(MobRelicSourceDefinition source, MobRelicTier tier, int mechanicOrdinal, out byte[] digest, out ulong seed, out MobRelicAspect aspect, out int packedColor, out float damage, out int crit, out float movement, out float attackSpeed, out float manaCost, out int defense, out float endurance, out int lifeRegen, out int maxLife, out int maxMana, out float echoChance, out float echoDamage, out int echoCooldown, out int wardDefense, out float wardEndurance, out int wardDuration, out MobRelicTrigger trigger, out MobRelicInvocation invocation, out MobRelicTwist twist, out byte hitCadence, out int scriptCooldown, out int scriptDuration, out float scriptPower)
	{
		if (mechanicOrdinal < 0)
			throw new ArgumentOutOfRangeException(nameof(mechanicOrdinal));
		digest = SHA256.HashData(Encoding.UTF8.GetBytes($"{source.ContentKey}|{(byte)tier}|mechanics-v2"));
		seed = BitConverter.ToUInt64(digest, 0);
		aspect = (MobRelicAspect)(digest[8] % Enum.GetValues<MobRelicAspect>().Length);
		int red = 72 + digest[9] % 184;
		int green = 72 + digest[10] % 184;
		int blue = 72 + digest[11] % 184;
		packedColor = (red << 16) | (green << 8) | blue;
		damage = 0f;
		crit = 0;
		movement = 0f;
		attackSpeed = 0f;
		manaCost = 0f;
		defense = 0;
		endurance = 0f;
		lifeRegen = 0;
		maxLife = 0;
		maxMana = 0;
		echoChance = 0f;
		echoDamage = 0f;
		echoCooldown = 48;
		wardDefense = 0;
		wardEndurance = 0f;
		wardDuration = 0;
		if (mechanicOrdinal >= MobRelicSourceCatalog.ExpectedItemCount)
			throw new ArgumentOutOfRangeException(nameof(mechanicOrdinal), "The registered relic source catalog was exceeded.");
		// One deliberate state machine per book. The first twenty ordinals consume
		// every trigger and every invocation exactly once; all sixteen twists are
		// represented. Acquisition relics reuse that vocabulary deterministically.
		trigger = (MobRelicTrigger)(mechanicOrdinal % TriggerCount);
		invocation = (MobRelicInvocation)(mechanicOrdinal % InvocationCount);
		twist = (MobRelicTwist)(mechanicOrdinal % TwistCount);
		hitCadence = (byte)(2 + digest[23] % 5);
		scriptCooldown = Math.Max(24, 132 - (int)tier * 18 + digest[24] % 37);
		scriptDuration = 90 + (int)tier * 24 + digest[25] % 91;
		scriptPower = 0.16f + (int)tier * 0.055f + digest[26] % 61 / 1000f;
	}
}
