using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
/// <summary>
/// Applies deterministic client prediction and validates the same decision on
/// the server before the vanilla DamageNPC packet is allowed to mutate life.
/// </summary>
public sealed partial class DamageAuthoritySystem
{
	internal static bool CanApplyVanillaDamage(DamageApprovalResult result) => result == DamageApprovalResult.Consumed;
	private static DamageHitLedger GetServerLedger(int sender)
	{
		if (!ServerPlayerLedgers.TryGetValue(sender, out DamageHitLedger ledger))
		{
			ledger = new DamageHitLedger();
			ServerPlayerLedgers.Add(sender, ledger);
		}

		return ledger;
	}

	private static void CommitServerLedger(int sender, DamageHitLedger tentativeLedger) => GetServerLedger(sender).ReplaceWith(tentativeLedger);

	private static bool CanDamageFriendly(Player player, NPC target, Item item) =>
		(target.type == NPCID.Guide && player.killGuide) || (target.type == NPCID.Clothier && player.killClothier) || (target.isLikeATownNPC && item.type == ItemID.Flymeal);

	private static void ApplyVanillaItemPayloadModifiers(Player player, NPC target, Item item, ref NPC.HitModifiers modifiers)
	{
		if (player.parryDamageBuff && item.DamageType.CountsAsClass(DamageClass.Melee))
			modifiers.ScalingBonusDamage += 4f;
		float sourceScale = 1f;
		if (item.type == ItemID.BreakerBlade && target.life >= target.lifeMax * 0.9f)
			sourceScale = 2.5f;
		else if (item.type == ItemID.HamBat)
		{
			int food = player.HasBuff(BuffID.WellFed3) ? 3 : player.HasBuff(BuffID.WellFed2) ? 2 : player.HasBuff(BuffID.WellFed) ? 1 : 0;
			sourceScale = 1f + 0.05f * food;
		}
		else if (item.type == ItemID.Keybrand && target.lifeMax > 0)
			sourceScale = 1f + Utils.GetLerpValue(1f, 0.1f, target.life / (float)target.lifeMax, true);
		modifiers.SourceDamage *= sourceScale;
		if (item.type == ItemID.Flymeal && target.isLikeATownNPC)
		{
			modifiers.ScalingArmorPenetration += 1f;
			if (target.type == NPCID.Guide)
				modifiers.TargetDamageMultiplier *= 2f;
		}
	}
}
