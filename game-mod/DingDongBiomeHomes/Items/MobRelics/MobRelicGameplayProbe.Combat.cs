using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes.Items.MobRelics;

internal sealed partial class MobRelicGameplayProbe
{
	private bool PrepareTarget()
	{
		stage = "target";
		targetIndex = Array.FindIndex(Main.npc, npc => npc == null || !npc.active);
		if (targetIndex < 0)
		{
			failure = "no-free-npc-slot";
			return false;
		}
		previousTarget = Main.npc[targetIndex];
		target = new NPC { whoAmI = targetIndex, active = true, friendly = false };
		target.SetDefaults(NPCID.BlueSlime);
		target.whoAmI = targetIndex;
		target.active = true;
		target.friendly = false;
		target.Center = player.Center + Vector2.UnitX * 96f;
		Main.npc[targetIndex] = target;
		activeProjectiles = Main.projectile.Where(projectile => projectile != null && projectile.active).Select(projectile => projectile.whoAmI).ToHashSet();
		Main.myPlayer = player.whoAmI;
		return true;
	}

	private bool SpawnEcho()
	{
		stage = "echo-spawn";
		int echoIndex = relicPlayer.ForceReleaseEchoForProbe(target, 120);
		spawnedEcho = echoIndex >= 0 && echoIndex < Main.projectile.Length ? Main.projectile[echoIndex] : null;
		if (spawnedEcho?.ModProjectile is MobRelicResonanceProjectile && !activeProjectiles.Contains(echoIndex) && spawnedEcho.damage > 0)
			return true;
		failure = "echo-spawn";
		return false;
	}

	private bool ExerciseEchoDamage()
	{
		stage = "echo-damage";
		NPC.HitModifiers modifiers = target.GetIncomingStrikeModifiers(spawnedEcho.DamageType, 1, false);
		DamageAuthoritySystem.ApplyProjectileHit(target, spawnedEcho, ref modifiers);
		float damage = modifiers.FinalDamage.ApplyTo(spawnedEcho.damage);
		((MobRelicResonanceProjectile)spawnedEcho.ModProjectile).OnHitNPC(target, default, Math.Max(1, (int)damage));
		if (damage > 0f && target.buffType.Any(type => type > 0))
			return true;
		failure = "echo-damage-or-aspect";
		return false;
	}
}
