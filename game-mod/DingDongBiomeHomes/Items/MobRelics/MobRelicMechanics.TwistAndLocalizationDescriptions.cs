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
	private static string TwistSummary(MobRelicEffectProfile profile, bool ru) => profile.Twist switch
	{
		MobRelicTwist.Quickened => ru ? "быстрый откат, сниженная сила" : "faster cooldown at reduced power",
		MobRelicTwist.Patient => ru ? "долгий откат, усиленный результат" : "longer cooldown with amplified power",
		MobRelicTwist.BloodPrice => ru ? "тратит немного здоровья, но сильно усиливает эффект" : "spends a little life to strongly amplify the effect",
		MobRelicTwist.ManaPrice => ru ? "тратит 8 маны, но усиливает эффект" : "spends 8 mana to amplify the effect",
		MobRelicTwist.Executioner => ru ? "усиливается против раненых целей" : "grows stronger against wounded targets",
		MobRelicTwist.FirstChapter => ru ? "усиливается против почти здоровых целей" : "grows stronger against nearly healthy targets",
		MobRelicTwist.Chainbound => ru ? "добавляет цепной заряд к соседней цели" : "adds a chained bolt toward a nearby target",
		MobRelicTwist.Rebound => ru ? "после срабатывания ускоряет владельца от цели" : "kicks the wielder away from the target",
		MobRelicTwist.Hearthguard => ru ? "после срабатывания кратко укрепляет защиту" : "briefly reinforces defense after activation",
		MobRelicTwist.Echoed => ru ? "повторяет часть эффекта вторым зарядом" : "echoes part of the effect with a second bolt",
		MobRelicTwist.Sustaining => ru ? "каждое срабатывание дополнительно лечит" : "each activation also restores life",
		MobRelicTwist.Overflowing => ru ? "каждое срабатывание дополнительно возвращает ману" : "each activation also restores mana",
		MobRelicTwist.Classweave => ru ? "смена класса урона усиливает результат" : "alternating damage classes amplifies the result",
		MobRelicTwist.Berserk => ru ? "сила растёт при низком здоровье" : "power rises at low life",
		MobRelicTwist.Serene => ru ? "сила растёт при полном здоровье" : "power rises near full life",
		MobRelicTwist.Clockwork => ru ? $"откат меняется по личному ритму {2 + profile.MechanicOrdinal % 7}" : $"cooldown follows personal rhythm {2 + profile.MechanicOrdinal % 7}",
		_ => ru ? "без модификатора" : "no twist"
	};
	internal static string SynergySummary(MobRelicSourceDefinition source) => Text("Synergy", MobRelicNaming.SourceDisplayName(source), source.Provider);
	internal static string DropSummary(MobRelicSourceDefinition source, MobRelicTier tier) => Text("UniqueDrop", Availability(tier), MobRelicNaming.SourceDisplayName(source), source.Provider);
	internal static string Availability(MobRelicTier tier) => tier switch
	{
		MobRelicTier.CommonTrace => Text("Availability.CommonTrace"),
		MobRelicTier.CommonCurio => Text("Availability.CommonCurio"),
		MobRelicTier.ExpertSeal => Text("Availability.ExpertSeal"),
		MobRelicTier.MasterRelic => Text("Availability.MasterRelic"),
		MobRelicTier.LegendaryMyth => Text("Availability.LegendaryMyth"),
		_ => "Unknown drop"
	};
	internal static string AspectName(MobRelicAspect aspect) => Text($"Aspect.{aspect}");
	private static string Percent(float value) => (value * 100f).ToString("0.#", CultureInfo.InvariantCulture) + "%";
	private static string Text(string key, params object[] args) => Language.GetTextValue($"Mods.DingDongBiomeHomes.MobRelics.Mechanics.{key}", args);
}
