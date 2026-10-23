using System;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.MobRelics;

internal sealed partial class MobRelicGameplayProbe
{
	private bool EquipConvergedSet()
	{
		stage = "equipment";
		MobRelicSet set = MobSpecificRelicCatalog.RegisteredSets.OrderBy(pair => pair.Key, StringComparer.Ordinal).First().Value;
		float damageBefore = player.GetDamage(DamageClass.Generic).Additive;
		float critBefore = player.GetCritChance(DamageClass.Generic);
		float movementBefore = player.moveSpeed;
		int defenseBefore = player.statDefense;
		int lifeBefore = player.statLifeMax2;
		relicPlayer.ResetEffects();
		foreach (GeneratedMobRelicItem relic in set.Items)
			relic.UpdateAccessory(player, hideVisual: false);
		relicPlayer.UpdateEquips();
		bool setSize = relicPlayer.StrongestSetSize == MobRelicMechanics.ConvergenceThreshold;
		bool echo = relicPlayer.EchoEnabled;
		bool ward = relicPlayer.WardEnabled;
		bool convergence = relicPlayer.FullConvergenceEnabled;
		bool damage = player.GetDamage(DamageClass.Generic).Additive > damageBefore;
		bool crit = player.GetCritChance(DamageClass.Generic) > critBefore;
		bool movement = player.moveSpeed > movementBefore;
		bool defense = player.statDefense > defenseBefore;
		bool life = player.statLifeMax2 > lifeBefore;
		if (setSize && echo && ward && convergence && damage && crit && movement && defense && life)
			return true;
		failure = $"equip-or-convergence:set={setSize},echo={echo},ward={ward},convergence={convergence},damage={damage},crit={crit},movement={movement},defense={defense},life={life}";
		return false;
	}

	private bool ExerciseWard()
	{
		stage = "ward";
		int defenseBefore = player.statDefense;
		relicPlayer.PostHurt(default);
		relicPlayer.UpdateEquips();
		if (relicPlayer.WardActiveTicks > 0 && player.statDefense > defenseBefore)
			return true;
		failure = "ward";
		return false;
	}
}
