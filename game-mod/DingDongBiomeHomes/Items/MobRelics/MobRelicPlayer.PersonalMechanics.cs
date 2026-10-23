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
	private void InvokePersonalMechanic(MobRelicEffectProfile profile, MobRelicScriptState state, NPC target, int damageDone, int damageMask)
	{
		float power = profile.ScriptPower;
		int cooldown = profile.ScriptCooldownTicks;
		switch (profile.Twist)
		{
			case MobRelicTwist.Quickened:
				power *= 0.82f;
				cooldown = (int)(cooldown * 0.72f);
				break;
			case MobRelicTwist.Patient:
				power *= 1.35f;
				cooldown = (int)(cooldown * 1.30f);
				break;
			case MobRelicTwist.BloodPrice:
				int lifeCost = Math.Max(1, Player.statLifeMax2 / 50);
				if (Player.statLife <= lifeCost)
					return;
				Player.statLife -= lifeCost;
				power *= 1.55f;
				break;
			case MobRelicTwist.ManaPrice:
				if (Player.statMana < 8)
					return;
				Player.statMana -= 8;
				power *= 1.45f;
				break;
			case MobRelicTwist.Executioner:
				if (target.lifeMax > 0 && target.life <= target.lifeMax * 0.30f)
					power *= 1.45f;
				break;
			case MobRelicTwist.FirstChapter:
				if (target.lifeMax > 0 && target.life + damageDone >= target.lifeMax * 0.90f)
					power *= 1.45f;
				break;
			case MobRelicTwist.Classweave:
				if (damageMask != 0)
					power *= 1.25f;
				break;
			case MobRelicTwist.Berserk:
				power *= 1f + 0.50f * (1f - Player.statLife / (float)Math.Max(1, Player.statLifeMax2));
				break;
			case MobRelicTwist.Serene:
				power *= 1f + 0.35f * Player.statLife / Math.Max(1f, Player.statLifeMax2);
				break;
			case MobRelicTwist.Clockwork:
				cooldown = Math.Max(18, cooldown - (2 + profile.MechanicOrdinal % 7) * 3);
				break;
		}

		int amount = Math.Max(1, (int)Math.Round(damageDone * power, MidpointRounding.AwayFromZero));
		switch (profile.Invocation)
		{
			case MobRelicInvocation.ResonantBolt:
				SpawnBolt(profile, target, amount, 0f);
				break;
			case MobRelicInvocation.TwinArc:
				SpawnBolt(profile, target, Math.Max(1, amount * 3 / 5), -0.18f);
				SpawnBolt(profile, target, Math.Max(1, amount * 3 / 5), 0.18f);
				break;
			case MobRelicInvocation.PrismaticFan:
				SpawnBolt(profile, target, Math.Max(1, amount / 2), -0.30f);
				SpawnBolt(profile, target, Math.Max(1, amount / 2), 0f);
				SpawnBolt(profile, target, Math.Max(1, amount / 2), 0.30f);
				break;
			case MobRelicInvocation.ElementalBrand:
				target.AddBuff(AspectBuff(profile.Aspect), 90 + profile.ScriptDurationTicks / 2);
				break;
			case MobRelicInvocation.Mend:
				RestoreLife(Math.Clamp(amount / 4, 1, Math.Max(2, Player.statLifeMax2 / 12)));
				break;
			case MobRelicInvocation.ManaBloom:
				RestoreMana(Math.Clamp(amount / 3, 2, Math.Max(4, Player.statManaMax2 / 8)));
				break;
			case MobRelicInvocation.Leech:
				RestoreLife(Math.Clamp(amount / 6, 1, Math.Max(2, Player.statLifeMax2 / 20)));
				SpawnBolt(profile, target, Math.Max(1, amount / 2), 0f);
				break;
			case MobRelicInvocation.Shockwave:
				SpawnBolt(profile, target, amount, 0f);
				target.AddBuff(AspectBuff(profile.Aspect), 60);
				break;
			case MobRelicInvocation.PhaseStep:
				Vector2 away = Player.Center - target.Center;
				if (away.LengthSquared() < 0.01f)
					away = Vector2.UnitX * -Player.direction;
				away.Normalize();
				Player.velocity = Vector2.Lerp(Player.velocity, away * (5f + power * 4f), 0.75f);
				state.ActiveTicks = Math.Max(state.ActiveTicks, 24);
				break;
			case MobRelicInvocation.ClockReset:
				foreach (MobRelicScriptState other in scriptStates.Values)
					if (!ReferenceEquals(other, state))
						other.Cooldown = Math.Max(0, other.Cooldown - 18 - (int)(power * 30f));
				break;
			default:
				state.ActiveTicks = Math.Max(state.ActiveTicks, profile.ScriptDurationTicks);
				break;
		}

		ApplyTwistAfter(profile, state, target, amount);
		state.Cooldown = Math.Max(18, cooldown);
	}
}
