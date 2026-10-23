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
internal static partial class MobRelicMechanics
{
	private static string InvocationSummary(MobRelicEffectProfile profile, bool ru) => profile.Invocation switch
	{
		MobRelicInvocation.ResonantBolt => ru ? "выпускает самонаводящийся резонансный болт" : "fires a homing resonance bolt",
		MobRelicInvocation.TwinArc => ru ? "выпускает два перекрёстных болта" : "fires two crossing bolts",
		MobRelicInvocation.PrismaticFan => ru ? "выпускает веер из трёх аспектных зарядов" : "fires a fan of three aspect charges",
		MobRelicInvocation.ElementalBrand => ru ? $"накладывает метку аспекта «{AspectName(profile.Aspect)}»" : $"brands the target with {AspectName(profile.Aspect)}",
		MobRelicInvocation.Mend => ru ? "лечит владельца пропорционально нанесённому урону" : "heals from a share of damage dealt",
		MobRelicInvocation.ManaBloom => ru ? "возвращает ману пропорционально нанесённому урону" : "restores mana from damage dealt",
		MobRelicInvocation.Bulwark => ru ? "создаёт временный оплот защиты" : "raises a temporary defensive bulwark",
		MobRelicInvocation.Haste => ru ? "даёт временное ускорение движения и атак" : "grants a temporary movement and attack-speed surge",
		MobRelicInvocation.Fury => ru ? "даёт временный прирост всего урона" : "grants a temporary all-damage surge",
		MobRelicInvocation.Precision => ru ? "даёт временный прирост шанса критического удара" : "grants temporary critical focus",
		MobRelicInvocation.Leech => ru ? "крадёт здоровье и выпускает слабый заряд" : "leeches life and releases a lesser bolt",
		MobRelicInvocation.ThresholdWard => ru ? "создаёт щит, усиливающийся после следующего ранения" : "primes a ward that strengthens after the next wound",
		MobRelicInvocation.Shockwave => ru ? "отбрасывает цель аспектной ударной волной" : "knocks the target back with an aspect shockwave",
		MobRelicInvocation.PhaseStep => ru ? "совершает короткий безопасный рывок от цели" : "phase-steps a short distance away from the target",
		MobRelicInvocation.Featherfall => ru ? "временно ослабляет гравитацию и урон от падения" : "temporarily softens gravity and fall damage",
		MobRelicInvocation.Regeneration => ru ? "запускает краткую усиленную регенерацию" : "starts a short regeneration burst",
		MobRelicInvocation.Endurance => ru ? "временно повышает сопротивление урону" : "temporarily raises damage resistance",
		MobRelicInvocation.Fortune => ru ? "создаёт короткое окно повышенной удачи" : "opens a brief window of increased luck",
		MobRelicInvocation.MixedSurge => ru ? "смешивает урон, скорость и защиту во временный импульс" : "mixes damage, speed, and defense into a temporary surge",
		MobRelicInvocation.ClockReset => ru ? "сокращает откаты остальных экипированных реликвий" : "reduces the cooldowns of other equipped relics",
		_ => ru ? "создаёт резонанс" : "releases resonance"
	};
}
