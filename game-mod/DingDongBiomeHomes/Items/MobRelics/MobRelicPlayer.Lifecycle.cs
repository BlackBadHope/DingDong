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
	public override void ResetEffects()
	{
		equippedSets.Clear();
		providerSources.Clear();
		activeProfiles.Clear();
		echoSetSize = 0;
		wardSetSize = 0;
		hasEcho = false;
		hasWard = false;
		fullConvergence = false;
	}

	public override void UpdateDead()
	{
		ResetEffects();
		echoCooldown = 0;
		wardCooldown = 0;
		wardActiveTicks = 0;
		afterHurtTicks = 0;
		scriptStates.Clear();
	}

	public override void UpdateEquips()
	{
		foreach (MobRelicEffectProfile profile in activeProfiles.Values)
		{
			if (scriptStates.TryGetValue(profile.MechanicsKey, out MobRelicScriptState state))
				ApplyActiveInvocation(profile, state);
		}

		foreach (MobRelicEquippedSet set in equippedSets.Values)
		{
			int count = set.DistinctTierCount;
			if (count >= MobRelicMechanics.LinkThreshold)
			{
				Player.GetDamage(DamageClass.Generic) += 0.005f * count;
				Player.moveSpeed += 0.005f * count;
			}

			if (count >= MobRelicMechanics.EchoThreshold && (!hasEcho || count > echoSetSize))
			{
				hasEcho = true;
				echoSetSize = count;
				echoAnchor = set.Anchor;
			}

			if (count >= MobRelicMechanics.WardThreshold && (!hasWard || count > wardSetSize))
			{
				hasWard = true;
				wardSetSize = count;
				wardAnchor = set.Anchor;
			}

			if (count >= MobRelicMechanics.ConvergenceThreshold)
				fullConvergence = true;
		}

		foreach (HashSet<string> sources in providerSources.Values)
		{
			if (sources.Count < MobRelicMechanics.ProviderHarmonyThreshold)
				continue;
			float harmony = Math.Min(0.06f, 0.01f * (sources.Count - 2));
			Player.GetDamage(DamageClass.Generic) += harmony;
			Player.moveSpeed += harmony * 0.5f;
		}

		if (fullConvergence)
		{
			Player.GetDamage(DamageClass.Generic) += 0.05f;
			Player.GetCritChance(DamageClass.Generic) += 5f;
			Player.statDefense += 5;
			Player.lifeRegen += 2;
		}

		if (hasWard && wardActiveTicks > 0)
		{
			Player.statDefense += Math.Max(wardAnchor.WardDefense, 4) + wardSetSize;
			Player.endurance += Math.Max(wardAnchor.WardEndurance, 0.04f) + 0.01f * (wardSetSize - 4);
			Player.lifeRegen += 2 + (fullConvergence ? 2 : 0);
			Player.noKnockback = true;
		}
	}

	public override void PostUpdate()
	{
		if (echoCooldown > 0)
			echoCooldown--;
		if (wardCooldown > 0)
			wardCooldown--;
		if (wardActiveTicks > 0)
			wardActiveTicks--;
		if (afterHurtTicks > 0)
			afterHurtTicks--;
		foreach (MobRelicScriptState state in scriptStates.Values)
		{
			if (state.Cooldown > 0)
				state.Cooldown--;
			if (state.ActiveTicks > 0)
				state.ActiveTicks--;
			if (state.TwistTicks > 0)
				state.TwistTicks--;
		}
	}

	private int SpawnBolt(MobRelicEffectProfile profile, NPC target, int damage, float angle)
	{
		Vector2 direction = target.Center - Player.Center;
		if (direction.LengthSquared() < 0.001f)
			direction = Vector2.UnitY;
		direction.Normalize();
		direction = direction.RotatedBy(angle);
		int aspectAndPhase = (int)profile.Aspect | (profile.MotionPhase << 8);
		return Projectile.NewProjectile(Player.GetSource_FromThis($"DingDongRelicScript{profile.MechanicOrdinal}"), Player.Center, direction * 8f, ModContent.ProjectileType<MobRelicResonanceProjectile>(), Math.Max(1, damage), 2f, Player.whoAmI, target.whoAmI, aspectAndPhase, profile.PackedColor);
	}
}
