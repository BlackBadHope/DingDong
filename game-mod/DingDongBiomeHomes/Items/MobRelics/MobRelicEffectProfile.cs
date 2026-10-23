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
internal readonly record struct MobRelicEffectProfile(MobRelicSourceDefinition Source, MobRelicTier Tier, MobRelicAspect Aspect, ulong ResonanceSeed, int PackedColor, byte MotionPhase, float DamageBonus, int CritBonus, float MoveSpeedBonus, float AttackSpeedBonus, float ManaCostReduction, int DefenseBonus, float EnduranceBonus, int LifeRegenBonus, int MaxLifeBonus, int MaxManaBonus, float EchoChance, float EchoDamageFactor, int EchoCooldownTicks, int WardDefense, float WardEndurance, int WardDurationTicks, int MechanicOrdinal, MobRelicTrigger Trigger, MobRelicInvocation Invocation, MobRelicTwist Twist, byte HitCadence, int ScriptCooldownTicks, int ScriptDurationTicks, float ScriptPower)
{
	internal string MechanicsKey => $"{Source.ContentKey}|{(byte)Tier}|{BehaviorSignature}";
	internal string BehaviorSignature => $"{Trigger}|{Invocation}|{Twist}";
	internal Color ResonanceColor => new((byte)((PackedColor >> 16) & 0xFF), (byte)((PackedColor >> 8) & 0xFF), (byte)(PackedColor & 0xFF));
	internal bool IsFunctional => DamageBonus > 0f || CritBonus > 0 || MoveSpeedBonus > 0f || AttackSpeedBonus > 0f || ManaCostReduction > 0f || DefenseBonus > 0 || EnduranceBonus > 0f || LifeRegenBonus > 0 || MaxLifeBonus > 0 || MaxManaBonus > 0 || EchoChance > 0f || WardDefense > 0 || ScriptPower > 0f;
}
