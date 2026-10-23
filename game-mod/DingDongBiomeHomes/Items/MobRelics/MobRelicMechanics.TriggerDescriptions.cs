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
	private static string TriggerSummary(MobRelicEffectProfile profile, bool ru) => profile.Trigger switch
	{
		MobRelicTrigger.AnyHit => ru ? "любой успешный удар" : "any successful hit",
		MobRelicTrigger.Critical => ru ? "критический удар" : "a critical hit",
		MobRelicTrigger.Cadence => ru ? $"каждый {profile.HitCadence}-й удар" : $"every {profile.HitCadence}th hit",
		MobRelicTrigger.Opening => ru ? "удар по почти здоровой цели" : "hitting a nearly healthy target",
		MobRelicTrigger.Finisher => ru ? "удар по цели ниже 30% здоровья" : "hitting a target below 30% life",
		MobRelicTrigger.Moving => ru ? "удар на высокой скорости" : "a hit while moving fast",
		MobRelicTrigger.Still => ru ? "удар, стоя на месте" : "a hit while standing still",
		MobRelicTrigger.Airborne => ru ? "удар в воздухе" : "an airborne hit",
		MobRelicTrigger.Grounded => ru ? "удар с земли" : "a grounded hit",
		MobRelicTrigger.Wounded => ru ? "удар при здоровье ниже половины" : "a hit below half life",
		MobRelicTrigger.Healthy => ru ? "удар при здоровье выше 90%" : "a hit above 90% life",
		MobRelicTrigger.LowMana => ru ? "удар при мане ниже трети" : "a hit below one-third mana",
		MobRelicTrigger.HighMana => ru ? "удар при мане выше 80%" : "a hit above 80% mana",
		MobRelicTrigger.AfterHurt => ru ? "ответный удар в течение 3 секунд после ранения" : "a retaliatory hit within 3 seconds of being hurt",
		MobRelicTrigger.CloseRange => ru ? "удар ближе 8 блоков" : "a hit within 8 tiles",
		MobRelicTrigger.LongRange => ru ? "удар дальше 24 блоков" : "a hit beyond 24 tiles",
		MobRelicTrigger.AlternatingClass => ru ? "смена класса урона между ударами" : "alternating damage classes between hits",
		MobRelicTrigger.RepeatTarget => ru ? $"{profile.HitCadence} последовательных удара по одной цели" : $"{profile.HitCadence} consecutive hits on one target",
		MobRelicTrigger.Daylight => ru ? "удар днём" : "a daytime hit",
		MobRelicTrigger.Nightfall => ru ? "удар ночью" : "a nighttime hit",
		_ => ru ? "удар" : "a hit"
	};
}
