using System;
using System.Collections.Generic;
using Terraria.GameContent.ItemDropRules;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class ThoriumMultipleRuleAdapter
{
	private static ThoriumMultipleRuleContract contract;
	private static readonly Dictionary<IItemDropRule, int> MutatedOriginals =
		new(ReferenceEqualityComparer.Instance);
	private static readonly HashSet<IItemDropRule> Preserved =
		new(ReferenceEqualityComparer.Instance);

	internal static void Initialize()
	{
		if (MutatedOriginals.Count != 0 || Preserved.Count != 0)
			throw Changed("initialize before restore");
		contract = ThoriumMultipleRuleContract.Load();
	}

	internal static bool TryInspect(IItemDropRule rule,
		out ThoriumMultipleSnapshot snapshot)
	{
		snapshot = default;
		if (contract is null || !contract.IsExactRule(rule)) return false;
		snapshot = contract.ReadAndValidate(rule);
		if (MutatedOriginals.TryGetValue(rule, out int originalDenominator))
			snapshot = snapshot with { Denominator = originalDenominator };
		foreach (IItemDropRule option in snapshot.Options)
			if (!HardLootRuleSemantics.PreservesGuarantee(
				HardLootRuleSemantics.Inspect(option)))
				throw Changed("selected child is not guaranteed");
		return true;
	}

	internal static bool TryNormalize(IItemDropRule rule)
		=> TryNormalize(rule, out _);

	internal static bool TryNormalize(IItemDropRule rule,
		out ThoriumMultipleSnapshot snapshot)
	{
		if (!TryInspect(rule, out snapshot)) return false;
		if (MutatedOriginals.ContainsKey(rule) || Preserved.Contains(rule))
		{
			HardLootAudit.NormalizedPreserved();
			return true;
		}
		if (snapshot.Numerator >= snapshot.Denominator)
		{
			Preserved.Add(rule);
			HardLootAudit.GuaranteedPreserved();
			return true;
		}
		int divisor = HardLootClassifier.ExtraDivisor(
			(float)snapshot.Numerator / snapshot.Denominator);
		if (divisor == 1)
		{
			Preserved.Add(rule);
			HardLootAudit.UltraRare();
			return true;
		}
		int hardened = HardLootPerPlayerRuleFactory.Denominator(
			snapshot.Denominator, divisor);
		MutatedOriginals.Add(rule, snapshot.Denominator);
		try
		{
			contract.WriteAndAssert(rule, hardened);
		}
		catch (Exception mutationError)
		{
			try
			{
				contract.WriteAndAssert(rule, snapshot.Denominator);
				MutatedOriginals.Remove(rule);
			}
			catch (Exception restoreError)
			{
				throw new AggregateException("Thorium multiple-rule rollback failed.",
					mutationError, restoreError);
			}
			throw;
		}
		HardLootAudit.Wrapped();
		return true;
	}

	internal static void RestoreAndReset()
	{
		List<Exception> failures = [];
		try
		{
			foreach ((IItemDropRule rule, int denominator) in MutatedOriginals)
				try { contract.WriteAndAssert(rule, denominator); }
				catch (Exception error) { failures.Add(error); }
		}
		finally
		{
			MutatedOriginals.Clear();
			Preserved.Clear();
			contract = null;
		}
		if (failures.Count != 0)
			throw new AggregateException("Thorium multiple rules could not be restored.", failures);
	}

	private static InvalidOperationException Changed(string surface) =>
		new($"Thorium multiple-rule lifecycle changed: {surface}.");
}
