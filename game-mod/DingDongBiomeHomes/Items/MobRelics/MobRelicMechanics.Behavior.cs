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
	internal static void ApplyPrimary(Player player, MobRelicEffectProfile profile)
	{
		if (profile.DamageBonus > 0f)
			player.GetDamage(DamageClass.Generic) += profile.DamageBonus;
		if (profile.CritBonus > 0)
			player.GetCritChance(DamageClass.Generic) += profile.CritBonus;
		if (profile.MoveSpeedBonus > 0f)
			player.moveSpeed += profile.MoveSpeedBonus;
		if (profile.AttackSpeedBonus > 0f)
			player.GetAttackSpeed(DamageClass.Generic) += profile.AttackSpeedBonus;
		if (profile.ManaCostReduction > 0f)
			player.manaCost = Math.Max(0.05f, player.manaCost - profile.ManaCostReduction);
		if (profile.DefenseBonus > 0)
			player.statDefense += profile.DefenseBonus;
		if (profile.EnduranceBonus > 0f)
			player.endurance += profile.EnduranceBonus;
		if (profile.LifeRegenBonus > 0)
			player.lifeRegen += profile.LifeRegenBonus;
		if (profile.MaxLifeBonus > 0)
			player.statLifeMax2 += profile.MaxLifeBonus;
		if (profile.MaxManaBonus > 0)
			player.statManaMax2 += profile.MaxManaBonus;
	}

	internal static string EffectSummary(MobRelicEffectProfile profile)
	{
		string passive = profile.Tier switch
		{
			MobRelicTier.CommonTrace => Text("Effect.CommonTrace", Percent(profile.DamageBonus), profile.CritBonus),
			MobRelicTier.CommonCurio => Text("Effect.CommonCurio", Percent(profile.MoveSpeedBonus), Percent(profile.AttackSpeedBonus), Percent(profile.ManaCostReduction)),
			MobRelicTier.ExpertSeal => Text("Effect.ExpertSeal", profile.DefenseBonus, Percent(profile.EnduranceBonus), profile.LifeRegenBonus),
			MobRelicTier.MasterRelic => Text("Effect.MasterRelic", Percent(profile.DamageBonus), Percent(profile.EchoChance), AspectName(profile.Aspect), Percent(profile.EchoDamageFactor)),
			MobRelicTier.LegendaryMyth => Text("Effect.LegendaryMyth", Percent(profile.DamageBonus), Percent(profile.MoveSpeedBonus), profile.MaxLifeBonus, profile.MaxManaBonus),
			_ => "Equippable generated relic."
		};
		return $"{passive} {UniqueMechanicSummary(profile)}";
	}

	internal static string UniqueMechanicSummary(MobRelicEffectProfile profile)
	{
		bool russian = Language.ActiveCulture?.Name?.StartsWith("ru", StringComparison.OrdinalIgnoreCase) == true;
		string trigger = TriggerSummary(profile, russian);
		string invocation = InvocationSummary(profile, russian);
		string twist = TwistSummary(profile, russian);
		float seconds = profile.ScriptCooldownTicks / 60f;
		return russian ? $"Личная механика #{profile.MechanicOrdinal + 1}: {trigger} → {invocation}. Модификатор: {twist}; откат {seconds:0.#} с." : $"Personal mechanic #{profile.MechanicOrdinal + 1}: {trigger} → {invocation}. Twist: {twist}; {seconds:0.#}s cooldown.";
	}
}
