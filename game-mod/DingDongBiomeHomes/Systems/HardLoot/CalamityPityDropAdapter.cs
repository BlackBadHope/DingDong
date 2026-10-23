using System;
using System.Collections.Generic;
using Terraria.GameContent.ItemDropRules;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class CalamityPityDropAdapter
{
	private static CalamityPityDropContract contract;
	private static readonly Dictionary<IItemDropRule, CalamityPityFraction> Originals =
		new(ReferenceEqualityComparer.Instance);

	internal static void Initialize()
	{
		if (Originals.Count != 0)
			throw Changed("initialize before restore");
		contract = CalamityPityDropContract.Load();
	}

	internal static bool TryNormalize(IItemDropRule rule)
	{
		if (contract is null || !contract.IsExactRule(rule)) return false;
		if (Originals.ContainsKey(rule))
		{
			HardLootAudit.NormalizedPreserved();
			return true;
		}
		CalamityPityFraction fraction = contract.ReadAndValidate(rule);
		if (fraction.StackCount == 1 || fraction.Numerator >= fraction.Denominator)
		{
			Originals.Add(rule, fraction);
			HardLootAudit.GuaranteedPreserved();
			return true;
		}
		int divisor = HardLootClassifier.ExtraDivisor(
			(float)fraction.Numerator / fraction.Denominator);
		if (divisor == 1)
		{
			Originals.Add(rule, fraction);
			HardLootAudit.UltraRare();
			return true;
		}
		int denominator = HardLootPerPlayerRuleFactory.Denominator(
			fraction.Denominator, divisor);
		object hardened = contract.NewFraction(fraction.Numerator, denominator);
		Originals.Add(rule, fraction);
		try
		{
			contract.WriteFraction(rule, hardened);
			contract.AssertFraction(rule, fraction.Numerator, denominator);
		}
		catch (Exception mutationError)
		{
			try
			{
				contract.WriteFraction(rule, fraction.Value);
				contract.AssertFraction(rule, fraction.Numerator, fraction.Denominator);
				Originals.Remove(rule);
			}
			catch (Exception restoreError)
			{
				throw new AggregateException("Calamity pity mutation rollback failed.",
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
			foreach ((IItemDropRule rule, CalamityPityFraction fraction) in Originals)
				try
				{
					contract.WriteFraction(rule, fraction.Value);
					contract.AssertFraction(rule, fraction.Numerator, fraction.Denominator);
				}
				catch (Exception error) { failures.Add(error); }
		}
		finally
		{
			Originals.Clear();
			contract = null;
		}
		if (failures.Count != 0)
			throw new AggregateException("Calamity pity drops could not be restored.", failures);
	}

	private static InvalidOperationException Changed(string surface) =>
		new($"Calamity pity-drop lifecycle changed: {surface}.");
}
