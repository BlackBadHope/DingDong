using System;
using System.Reflection;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class RandomLootGate
{
	private static object enemyOwner;
	private static object bossOwner;
	private static PropertyInfo enemyFlag;
	private static PropertyInfo bossFlag;

	internal static bool VerifiedDisabled { get; private set; }
	internal static bool IsDisabled => VerifiedDisabled && !Read(enemyOwner, enemyFlag) &&
		!Read(bossOwner, bossFlag);

	internal static void AssertNpcRandomizersDisabled()
	{
		Mod provider = HardLootProviderGate.Require("RandomLoot", new Version(3, 2, 2),
			"831ed611-4e95-4e5a-abd8-6ff05fc4e6a7");
		Type modType = provider.Code.GetType("RandomLoot.RandomLoot", true);
		PropertyInfo configProperty = RequireProperty(modType, "Config", true, requireSetter: false);
		object config = configProperty.GetValue(null) ?? throw Changed("Config is null");
		enemyOwner = RequireProperty(config.GetType(), "EnemyLootRandomizer", false, false).GetValue(config)
			?? throw Changed("EnemyLootRandomizer is null");
		bossOwner = RequireProperty(config.GetType(), "BossLootRandomizer", false, false).GetValue(config)
			?? throw Changed("BossLootRandomizer is null");
		enemyFlag = RequireProperty(enemyOwner.GetType(), "RandomEnemyLoot", false, true, expectBool: true);
		bossFlag = RequireProperty(bossOwner.GetType(), "RandomBossDrops", false, true, expectBool: true);
		if (Read(enemyOwner, enemyFlag) || Read(bossOwner, bossFlag))
			throw new InvalidOperationException("Hard-loot refuses enabled RandomLoot NPC randomizers at startup.");
		VerifiedDisabled = true;
	}

	internal static void EnforceBeforeLoot()
	{
		if (!VerifiedDisabled)
			throw new InvalidOperationException("RandomLoot gate was not initialized before NPC loot.");
		bool enemyDrift = Read(enemyOwner, enemyFlag);
		bool bossDrift = Read(bossOwner, bossFlag);
		if (!enemyDrift && !bossDrift) return;
		if (enemyDrift) enemyFlag.SetValue(enemyOwner, false);
		if (bossDrift) bossFlag.SetValue(bossOwner, false);
		if (!IsDisabled) throw Changed("runtime flags could not be neutralized in memory");
		HardLootAudit.RandomLootDrift();
	}

	private static bool Read(object owner, PropertyInfo property)
	{
		if (owner is null || property is null) return true;
		return (bool)property.GetValue(owner);
	}

	private static PropertyInfo RequireProperty(Type type, string name, bool isStatic,
		bool requireSetter, bool expectBool = false)
	{
		PropertyInfo property = type.GetProperty(name, BindingFlags.Public |
			BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
		if (property?.GetMethod is null || property.GetMethod.IsStatic != isStatic ||
			(expectBool && property.PropertyType != typeof(bool)) ||
			(requireSetter && (property.SetMethod is null || property.SetMethod.IsStatic != isStatic)))
			throw Changed(name);
		return property;
	}

	private static InvalidOperationException Changed(string surface) =>
		new($"RandomLoot 3.2.2 config surface changed at {surface}.");

	internal static void Reset()
	{
		enemyOwner = bossOwner = null;
		enemyFlag = bossFlag = null;
		VerifiedDisabled = false;
	}
}
