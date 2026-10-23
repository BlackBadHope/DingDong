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
	private bool TriggerMatches(MobRelicEffectProfile profile, MobRelicScriptState state, NPC target, NPC.HitInfo hit, int damageDone, int damageMask)
	{
		state.HitCount++;
		if (state.LastTarget == target.whoAmI)
			state.SameTargetHits++;
		else
		{
			state.LastTarget = target.whoAmI;
			state.SameTargetHits = 1;
		}

		bool alternated = state.LastDamageMask != 0 && damageMask != 0 && state.LastDamageMask != damageMask;
		if (damageMask != 0)
			state.LastDamageMask = damageMask;
		int lifeBefore = Math.Min(target.lifeMax, Math.Max(0, target.life) + damageDone);
		float targetLifeRatio = target.lifeMax <= 0 ? 0f : lifeBefore / (float)target.lifeMax;
		float playerLifeRatio = Player.statLifeMax2 <= 0 ? 0f : Player.statLife / (float)Player.statLifeMax2;
		float manaRatio = Player.statManaMax2 <= 0 ? 0f : Player.statMana / (float)Player.statManaMax2;
		float distanceSquared = Vector2.DistanceSquared(Player.Center, target.Center);
		return profile.Trigger switch
		{
			MobRelicTrigger.AnyHit => true,
			MobRelicTrigger.Critical => hit.Crit,
			MobRelicTrigger.Cadence => state.HitCount % profile.HitCadence == 0,
			MobRelicTrigger.Opening => targetLifeRatio >= 0.90f,
			MobRelicTrigger.Finisher => targetLifeRatio <= 0.30f,
			MobRelicTrigger.Moving => Player.velocity.LengthSquared() >= 36f,
			MobRelicTrigger.Still => Player.velocity.LengthSquared() <= 1f,
			MobRelicTrigger.Airborne => Player.velocity.Y != 0f,
			MobRelicTrigger.Grounded => Player.velocity.Y == 0f,
			MobRelicTrigger.Wounded => playerLifeRatio <= 0.50f,
			MobRelicTrigger.Healthy => playerLifeRatio >= 0.90f,
			MobRelicTrigger.LowMana => manaRatio <= 0.33f,
			MobRelicTrigger.HighMana => manaRatio >= 0.80f,
			MobRelicTrigger.AfterHurt => afterHurtTicks > 0,
			MobRelicTrigger.CloseRange => distanceSquared <= 128f * 128f,
			MobRelicTrigger.LongRange => distanceSquared >= 384f * 384f,
			MobRelicTrigger.AlternatingClass => alternated,
			MobRelicTrigger.RepeatTarget => state.SameTargetHits % profile.HitCadence == 0,
			MobRelicTrigger.Daylight => Main.dayTime,
			MobRelicTrigger.Nightfall => !Main.dayTime,
			_ => false
		};
	}

	private NPC FindChainTarget(NPC excluded)
	{
		NPC best = null;
		float bestDistance = 560f * 560f;
		foreach (NPC npc in Main.npc)
		{
			if (npc == null || !npc.active || npc.friendly || npc.whoAmI == excluded.whoAmI || !npc.CanBeChasedBy(Player))
				continue;
			float distance = Vector2.DistanceSquared(excluded.Center, npc.Center);
			if (distance < bestDistance)
			{
				bestDistance = distance;
				best = npc;
			}
		}

		return best;
	}
}
