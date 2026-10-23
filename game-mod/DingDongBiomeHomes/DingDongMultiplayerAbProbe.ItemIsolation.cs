using System;
using System.Collections.Generic;
using System.Linq;
using DingDongBiomeHomes.Items.AccessoryEnchanting;
using DingDongBiomeHomes.Items.MobRelics;
using DingDongBiomeHomes.Items.SynergyArmor;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;
internal static partial class DingDongMultiplayerAbProbe
{
	private static void RunItemIsolation(Player playerA, Player playerB, List<string> failures, ref int passed)
	{
		Vector2 positionA = playerA.position;
		Vector2 positionB = playerB.position;
		AccessoryEnchantPlayer accessoryA = playerA.GetModPlayer<AccessoryEnchantPlayer>();
		AccessoryEnchantPlayer accessoryB = playerB.GetModPlayer<AccessoryEnchantPlayer>();
		accessoryA.ResetEffects();
		accessoryB.ResetEffects();
		playerA.statDefense = Player.DefenseStat.Default;
		playerB.statDefense = Player.DefenseStat.Default;
		playerA.luck = 0f;
		playerB.luck = 0f;
		accessoryA.Apply(AccessoryEnchantmentId.Bastion);
		accessoryB.Apply(AccessoryEnchantmentId.Fortune);
		accessoryA.PostUpdateEquips();
		accessoryB.PostUpdateEquips();
		Check(failures, "items:accessory-isolation", playerA.statDefense >= 3 && playerA.luck < 0.01f && playerB.statDefense == Player.DefenseStat.Default && playerB.luck >= 0.119f, ref passed);
		RunStationIsolation(playerA, playerB, failures, ref passed);
		SynergyArmorSet[] armorSets = SynergyArmorCatalog.RegisteredSets.Values.OrderBy(set => set.Definition.Id).ToArray();
		SynergyArmorPlayer armorA = playerA.GetModPlayer<SynergyArmorPlayer>();
		SynergyArmorPlayer armorB = playerB.GetModPlayer<SynergyArmorPlayer>();
		armorA.ResetEffects();
		armorB.ResetEffects();
		SynergyArmorGlobalItem armorHook = new();
		ApplyArmorSet(armorHook, playerA, armorSets[0]);
		ApplyArmorSet(armorHook, playerB, armorSets[^1]);
		Check(failures, "items:armor-set-isolation", armorA.ActiveSetId == armorSets[0].Definition.Id && armorB.ActiveSetId == armorSets[^1].Definition.Id && armorA.ActiveSetId != armorB.ActiveSetId, ref passed);
		MobRelicPlayer relicA = playerA.GetModPlayer<MobRelicPlayer>();
		MobRelicPlayer relicB = playerB.GetModPlayer<MobRelicPlayer>();
		relicA.ResetEffects();
		relicB.ResetEffects();
		MobRelicSet relicSet = MobSpecificRelicCatalog.RegisteredSets.OrderBy(pair => pair.Key, StringComparer.Ordinal).First().Value;
		foreach (GeneratedMobRelicItem relic in relicSet.Items)
			relic.UpdateAccessory(playerA, hideVisual: false);
		relicA.UpdateEquips();
		relicB.UpdateEquips();
		Check(failures, "items:relic-convergence-isolation", relicA.FullConvergenceEnabled && !relicB.FullConvergenceEnabled && relicA.StrongestSetSize == MobRelicMechanics.ConvergenceThreshold && relicB.StrongestSetSize == 0, ref passed);
		armorA.PostUpdateEquips();
		armorB.PostUpdateEquips();
		Check(failures, "items:cross-system-synergy-isolation", armorA.RelicSourceCount == 1 && armorA.ProviderCount == 1 && armorB.RelicSourceCount == 0 && armorB.ProviderCount == 0, ref passed);
		Check(failures, "items:no-position-side-effect", playerA.position == positionA && playerB.position == positionB, ref passed);
	}

	private static void ApplyArmorSet(SynergyArmorGlobalItem hook, Player player, SynergyArmorSet set)
	{
		Item head = NewItem(set.Head.Type);
		Item body = NewItem(set.Body.Type);
		Item legs = NewItem(set.Legs.Type);
		string key = hook.IsArmorSet(head, body, legs);
		hook.UpdateArmorSet(player, key);
	}

	private static Item NewItem(int type)
	{
		Item item = new();
		item.SetDefaults(type);
		return item;
	}

	private static bool Equivalent(ResidentQuestProgress left, ResidentQuestProgress right) => left.Completed == right.Completed && left.Active == right.Active && left.Progress == right.Progress && left.ActiveQuestId == right.ActiveQuestId && left.ActiveKind == right.ActiveKind && left.ActiveItemType == right.ActiveItemType && left.ActiveGoal == right.ActiveGoal && string.Equals(left.ActiveTargetKey, right.ActiveTargetKey, StringComparison.Ordinal) && left.RewardCopper == right.RewardCopper;
}
