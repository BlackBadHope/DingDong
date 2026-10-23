using System;
using System.Linq;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Systems.HardLoot;

public sealed class HardLootGlobalNpc : GlobalNPC
{
	public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
	{
		if (npc.friendly || npc.townNPC)
		{
			HardLootAudit.Protected();
			return;
		}
		Harden(npcLoot.Get(includeGlobalDrops: false).ToArray(),
			rule => npcLoot.Remove(rule), rule => npcLoot.Add(rule),
			preserveBosses: false, scope: $"npc:{npc.type}");
	}

	public override void ModifyGlobalLoot(GlobalLoot globalLoot) =>
		Harden(globalLoot.Get(false).ToArray(),
			rule => globalLoot.Remove(rule), rule => globalLoot.Add(rule),
			preserveBosses: false, scope: "global");

	private static void Harden(
		IItemDropRule[] rules,
		Action<IItemDropRule> remove,
		Action<IItemDropRule> add,
		bool preserveBosses,
		string scope)
	{
		foreach (IItemDropRule rule in rules)
		{
			HardLootAudit.Visited();
			if (HardLootProviderRuleAdapters.TryNormalize(rule,
				out IItemDropRule providerRule))
			{
				if (!ReferenceEquals(rule, providerRule))
				{
					remove(rule);
					add(providerRule);
				}
				continue;
			}
			if (HardLootFailureFallbackNormalizer.TryNormalize(
				rule, 0, out IItemDropRule fallbackRule))
			{
				if (!ReferenceEquals(rule, fallbackRule))
				{
					remove(rule);
					add(fallbackRule);
				}
				continue;
			}
			if (HardLootLeadingConditionNormalizer.TryNormalize(
				rule, scope, 0, out IItemDropRule leadingRule))
			{
				if (leadingRule is not null)
				{
					remove(rule);
					add(leadingRule);
				}
				continue;
			}
			if (FargoSoulsDifficultyAdapter.TryNormalize(rule, preserveBosses, out IItemDropRule replacement))
			{
				if (replacement is not null)
				{
					remove(rule);
					add(replacement);
				}
				continue;
			}
			if (HardLootDifficultyNormalizer.TryNormalize(rule, preserveBosses))
				continue;
			HardLootDisposition result = HardLootClassifier.Inspect(rule, out HardLootLeaf leaf);
			if (result == HardLootDisposition.EmptyBranch)
			{
				HardLootAudit.DropNothingBranchPreserved();
				continue;
			}
			if (result == HardLootDisposition.Guaranteed)
			{
				HardLootAudit.GuaranteedPreserved();
				continue;
			}
			if (result == HardLootDisposition.Normalized)
			{
				HardLootAudit.NormalizedPreserved();
				continue;
			}
			if (result == HardLootDisposition.Protected)
			{
				HardLootAudit.Protected();
				continue;
			}
			if (result == HardLootDisposition.ReportFailed)
			{
				HardLootAudit.ReportFailed(scope, rule.GetType().FullName);
				continue;
			}
			if (result == HardLootDisposition.Unsupported)
			{
				HardLootAudit.Unsupported(scope, rule.GetType().FullName, leaf?.ItemSummary ?? "unknown");
				continue;
			}
			if (leaf.ExtraDivisor == 1)
			{
				HardLootAudit.UltraRare();
				continue;
			}
			remove(rule);
			IItemDropRule hardened = HardLootRuleFactory.Wrap(
				rule, leaf.ExtraDivisor, preserveBosses);
			add(hardened);
			HardLootAudit.Wrapped();
		}
	}
}
