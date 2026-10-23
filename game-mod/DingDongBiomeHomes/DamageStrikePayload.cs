using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
internal readonly record struct DamageStrikePayload(int Damage, int SourceDamage, int DamageType, int HitDirection, float Knockback, bool Crit, bool InstantKill, bool HideCombatText)
{
	public static DamageStrikePayload FromHit(in NPC.HitInfo hit) => new(hit.Damage, hit.SourceDamage, hit.DamageType.Type, hit.HitDirection, hit.Knockback, hit.Crit, hit.InstantKill, hit.HideCombatText);
	public bool IsValid => Damage >= 0 && SourceDamage >= 0 && DamageType >= 0 && DamageType < DamageClassLoader.DamageClassCount && HitDirection is >= sbyte.MinValue and <= sbyte.MaxValue && float.IsFinite(Knockback) && Math.Abs(Knockback) <= 1_000_000f;

	public bool MatchesExactly(DamageStrikePayload other) => Damage == other.Damage && SourceDamage == other.SourceDamage && DamageType == other.DamageType && HitDirection == other.HitDirection && BitConverter.SingleToInt32Bits(Knockback) == BitConverter.SingleToInt32Bits(other.Knockback) && Crit == other.Crit && InstantKill == other.InstantKill && HideCombatText == other.HideCombatText;
}
