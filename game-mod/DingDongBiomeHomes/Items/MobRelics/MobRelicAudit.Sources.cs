using System;
using System.Linq;
using Terraria;

namespace DingDongBiomeHomes.Items.MobRelics;

internal static partial class MobRelicAudit
{
	private static void AuditLoadedSources(MobRelicAuditContext context)
	{
		if (context.LoadedKeys.SetEquals(context.GeneratedKeys))
			return;
		foreach (string missing in context.GeneratedKeys.Except(context.LoadedKeys, StringComparer.Ordinal).Take(4))
			Failure(context, $"not-loaded:{missing}");
	}

	private static void AuditMobSource(MobRelicAuditContext context, NPC mob)
	{
		string key = QuestContentRegistry.ContentKey(mob);
		if (!context.Sets.TryGetValue(key, out MobRelicSet set))
		{
			Failure(context, $"set:{key}");
			return;
		}

		if (QuestContentRegistry.TryGetNpcTargetType(key, out int questType) && questType == mob.type)
			context.QuestIdentityPassed++;
		else
			Failure(context, $"quest-key:{key}");

		bool registered = context.Drops.TryGetValue(key, out MobRelicDropRegistration drop);
		bool valid = registered && drop.Boss == mob.boss
			&& drop.CommonDenominator == (mob.boss ? MobRelicGlobalNPC.BossCommonDenominator : MobRelicGlobalNPC.NormalCommonDenominator)
			&& drop.ExpertDenominator == (mob.boss ? MobRelicGlobalNPC.BossDifficultyDenominator : MobRelicGlobalNPC.NormalExpertDenominator)
			&& drop.MasterDenominator == (mob.boss ? MobRelicGlobalNPC.BossDifficultyDenominator : MobRelicGlobalNPC.NormalMasterDenominator)
			&& drop.LegendaryDenominator == (mob.boss ? MobRelicGlobalNPC.BossDifficultyDenominator : MobRelicGlobalNPC.NormalLegendaryDenominator)
			&& drop.CommonTraceType == set.CommonTrace.Type && drop.CommonCurioType == set.CommonCurio.Type
			&& drop.ExpertType == set.ExpertSeal.Type && drop.MasterType == set.MasterRelic.Type && drop.LegendaryType == set.LegendaryMyth.Type
			&& set.Items.Count == MobSpecificRelicCatalog.ItemsPerMob && set.Items.Select(item => item.Type).Distinct().Count() == MobSpecificRelicCatalog.ItemsPerMob
			&& set.Source.ContentKey == key && set.Source.Boss == mob.boss;
		if (valid)
			context.PoolsPassed++;
		else
			Failure(context, $"drop:{key}");
		AuditLootDatabase(context, mob, key, set, drop);
	}
}
