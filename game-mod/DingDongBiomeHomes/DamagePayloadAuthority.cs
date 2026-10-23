using System;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
internal readonly record struct DamagePayloadEnvelope(int DamageType, int MaxSourceDamage, int MaxDamage, float MaxKnockback, bool AllowCrit, bool AllowInstantKill, bool AllowHiddenCombatText)
{
	public bool Enforced => MaxSourceDamage > 0 && MaxDamage > 0;
	public bool Accepts(in DamageStrikePayload payload)
	{
		if (!Enforced || !payload.IsValid || payload.DamageType != DamageType)
			return false;
		if (payload.Damage is < 1 || payload.Damage > MaxDamage || payload.SourceDamage is < 1 || payload.SourceDamage > MaxSourceDamage)
			return false;
		if (payload.HitDirection is < -1 or > 1 || payload.Knockback < 0f || payload.Knockback > MaxKnockback)
			return false;
		return (!payload.Crit || AllowCrit) && (!payload.InstantKill || AllowInstantKill) && (!payload.HideCombatText || AllowHiddenCombatText);
	}
}

internal static class DamagePayloadAuthority
{
	public static bool TryBuild(DamageClass damageType, int baseDamage, float knockback, bool mayCrit, NPC.HitModifiers modifiers, out DamagePayloadEnvelope envelope)
	{
		envelope = default;
		if (baseDamage <= 0 || !float.IsFinite(knockback) || knockback < 0f)
			return false;
		NPC.HitInfo normal = modifiers.ToHitInfo(baseDamage, false, knockback, false, 0f);
		NPC.HitInfo critical = mayCrit ? modifiers.ToHitInfo(baseDamage, true, knockback, false, 0f) : normal;
		int maxSource = AddVariation(Math.Max(normal.SourceDamage, critical.SourceDamage));
		int maxDamage = AddVariation(Math.Max(normal.Damage, critical.Damage));
		float maxKnockback = Math.Max(Math.Abs(normal.Knockback), Math.Abs(critical.Knockback)) * 1.2f + 0.25f;
		if (maxSource <= 0 || maxDamage <= 0 || !float.IsFinite(maxKnockback))
			return false;
		envelope = new DamagePayloadEnvelope(damageType.Type, maxSource, maxDamage, maxKnockback, normal.Crit || critical.Crit, normal.InstantKill || critical.InstantKill, normal.HideCombatText || critical.HideCombatText);
		return true;
	}

	private static int AddVariation(int value)
	{
		double upper = value * 1.2d + 2d;
		return value <= 0 ? 0 : upper >= int.MaxValue ? int.MaxValue : (int)Math.Ceiling(upper);
	}
}
