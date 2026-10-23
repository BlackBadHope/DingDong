using System;
using System.Reflection;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class LootBonusNeutralizer
{
	private static object config;
	private static FieldInfo multiplier;
	private static FieldInfo multiplyCoins;
	private static int originalMultiplier;
	private static bool originalCoins;
	private static bool captured;

	internal static bool Applied { get; private set; }
	internal static bool IsNeutralized => Applied && ReadMultiplier() == 1 && !ReadCoins();

	internal static void Apply()
	{
		Mod provider = HardLootProviderGate.Require("LootBonus", new Version(6, 0),
			"efc015f6-34c0-4156-a91d-91da9dbc19dc");
		Type configType = provider.Code?.GetType("LootBonus.LootConfig", false);
		FieldInfo instanceField = RequireField(configType, "Instance", typeof(object), true);
		config = instanceField.GetValue(null)
			?? throw new InvalidOperationException("LootBonus.LootConfig.Instance is null.");
		multiplier = RequireField(configType, "LootMulti", typeof(int), false);
		multiplyCoins = RequireField(configType, "MultiplyCoins", typeof(bool), false);
		if (!captured)
		{
			originalMultiplier = ReadMultiplier();
			originalCoins = ReadCoins();
			captured = true;
		}
		SetNeutral();
		Applied = true;
	}

	internal static void EnforceBeforeLoot()
	{
		if (!Applied)
			throw new InvalidOperationException("LootBonus neutralizer was not initialized before NPC loot.");
		if (IsNeutralized) return;
		SetNeutral();
		HardLootAudit.LootBonusDrift();
	}

	private static void SetNeutral()
	{
		multiplier.SetValue(config, 1);
		multiplyCoins.SetValue(config, false);
		if (ReadMultiplier() != 1 || ReadCoins())
			throw new InvalidOperationException("LootBonus 6.0 in-memory neutralization did not stick.");
	}

	private static int ReadMultiplier() => multiplier is null || config is null
		? int.MinValue : (int)multiplier.GetValue(config);
	private static bool ReadCoins() => multiplyCoins is not null && config is not null &&
		(bool)multiplyCoins.GetValue(config);

	private static FieldInfo RequireField(Type type, string name, Type expectedType, bool isStatic)
	{
		if (type is null) throw new InvalidOperationException("LootBonus 6.0 config type is missing.");
		FieldInfo field = type.GetField(name, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
		bool typeMatches = expectedType == typeof(object) || field?.FieldType == expectedType;
		if (field is null || field.IsStatic != isStatic || !typeMatches)
			throw new InvalidOperationException($"LootBonus 6.0 API mismatch at {name}.");
		return field;
	}

	internal static void RestoreAndReset()
	{
		if (captured && config is not null && multiplier is not null && multiplyCoins is not null)
		{
			multiplier.SetValue(config, originalMultiplier);
			multiplyCoins.SetValue(config, originalCoins);
		}
		config = null;
		multiplier = multiplyCoins = null;
		captured = Applied = false;
	}
}
