using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
public sealed class DamageCombatGlobalNPC : GlobalNPC
{
	public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers) => DamageAuthoritySystem.ApplyItemHit(npc, player, item, ref modifiers);
	public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers) => DamageAuthoritySystem.ApplyProjectileHit(npc, projectile, ref modifiers);
}
