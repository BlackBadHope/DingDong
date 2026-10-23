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
public sealed partial class MobRelicPlayer
{
	private void ApplyTwistAfter(MobRelicEffectProfile profile, MobRelicScriptState state, NPC target, int amount)
	{
		switch (profile.Twist)
		{
			case MobRelicTwist.Chainbound:
				NPC chainTarget = FindChainTarget(target);
				if (chainTarget != null)
					SpawnBolt(profile, chainTarget, Math.Max(1, amount / 2), 0f);
				break;
			case MobRelicTwist.Rebound:
				Vector2 rebound = Player.Center - target.Center;
				if (rebound.LengthSquared() > 0.01f)
				{
					rebound.Normalize();
					Player.velocity += rebound * 3.5f;
				}

				break;
			case MobRelicTwist.Hearthguard:
				state.TwistTicks = Math.Max(state.TwistTicks, 120);
				break;
			case MobRelicTwist.Echoed:
				SpawnBolt(profile, target, Math.Max(1, amount / 3), 0.12f);
				break;
			case MobRelicTwist.Sustaining:
				RestoreLife(Math.Max(1, Player.statLifeMax2 / 100));
				break;
			case MobRelicTwist.Overflowing:
				RestoreMana(Math.Max(2, Player.statManaMax2 / 40));
				break;
		}
	}

	private void ApplyActiveInvocation(MobRelicEffectProfile profile, MobRelicScriptState state)
	{
		if (state.TwistTicks > 0)
		{
			Player.statDefense += 3 + (int)(profile.ScriptPower * 8f);
			Player.endurance += 0.02f;
		}

		if (state.ActiveTicks <= 0)
			return;
		float strength = profile.ScriptPower;
		switch (profile.Invocation)
		{
			case MobRelicInvocation.Bulwark:
				Player.statDefense += 5 + (int)(strength * 18f);
				Player.endurance += 0.03f + strength * 0.04f;
				break;
			case MobRelicInvocation.Haste:
				Player.moveSpeed += 0.08f + strength * 0.12f;
				Player.GetAttackSpeed(DamageClass.Generic) += 0.05f + strength * 0.08f;
				break;
			case MobRelicInvocation.Fury:
				Player.GetDamage(DamageClass.Generic) += 0.06f + strength * 0.14f;
				break;
			case MobRelicInvocation.Precision:
				Player.GetCritChance(DamageClass.Generic) += 4f + strength * 12f;
				break;
			case MobRelicInvocation.ThresholdWard:
				Player.statDefense += afterHurtTicks > 0 ? 12 : 6;
				Player.endurance += afterHurtTicks > 0 ? 0.08f : 0.035f;
				break;
			case MobRelicInvocation.Featherfall:
				Player.gravity *= 0.70f;
				Player.maxFallSpeed *= 0.75f;
				Player.noFallDmg = true;
				break;
			case MobRelicInvocation.Regeneration:
				Player.lifeRegen += 4 + (int)(strength * 12f);
				break;
			case MobRelicInvocation.Endurance:
				Player.endurance += 0.04f + strength * 0.08f;
				break;
			case MobRelicInvocation.Fortune:
				Player.luck += 0.08f + strength * 0.18f;
				break;
			case MobRelicInvocation.MixedSurge:
				Player.GetDamage(DamageClass.Generic) += 0.04f + strength * 0.06f;
				Player.moveSpeed += 0.06f + strength * 0.08f;
				Player.statDefense += 3 + (int)(strength * 8f);
				break;
		}
	}

	private void RestoreLife(int amount)
	{
		int restored = Math.Min(Math.Max(0, amount), Math.Max(0, Player.statLifeMax2 - Player.statLife));
		if (restored <= 0)
			return;
		Player.statLife += restored;
		Player.HealEffect(restored, true);
		SyncVitals();
	}

	private void RestoreMana(int amount)
	{
		int restored = Math.Min(Math.Max(0, amount), Math.Max(0, Player.statManaMax2 - Player.statMana));
		if (restored <= 0)
			return;
		Player.statMana += restored;
		Player.ManaEffect(restored);
		SyncVitals();
	}

	private static int AspectBuff(MobRelicAspect aspect) => aspect switch
	{
		MobRelicAspect.Cinder => BuffID.OnFire,
		MobRelicAspect.Frost => BuffID.Frostburn,
		MobRelicAspect.Storm => BuffID.Electrified,
		MobRelicAspect.Venom => BuffID.Poisoned,
		MobRelicAspect.Radiant => BuffID.Ichor,
		MobRelicAspect.Umbral => BuffID.ShadowFlame,
		MobRelicAspect.Tidal => BuffID.Wet,
		MobRelicAspect.Verdant => BuffID.Venom,
		_ => BuffID.OnFire
	};
}
