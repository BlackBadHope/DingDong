using System;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class HardLootProviderGate
{
	private const string LootBonusMvid = "efc015f6-34c0-4156-a91d-91da9dbc19dc";
	private const string FargoSoulsMvid = "535d0777-d3df-4436-ac3f-e1d1fd0ad83b";
	private const string ImproveGameMvid = "b3326788-dd23-4981-b017-a9b659007680";
	private const string SotsMvid = "14c19a4a-afb8-49fb-ab76-cd75dff954ac";
	private const string CalamityMvid = "0e63c698-ec72-446e-ac1f-8d01f0ee7ac7";
	private const string ThoriumMvid = "c3026632-3918-4387-be6d-aa5539117e51";
	private const string PackBuilderMvid = "f9ed0f20-1e03-42a5-90de-f0ed24a2d906";
	private const string RandomLootMvid = "831ed611-4e95-4e5a-abd8-6ff05fc4e6a7";
	private const string FargoMutantMvid = "c9c57245-e2b1-4ad9-a0a3-d34e25bccf1c";

	internal static bool Verified { get; private set; }

	internal static void AssertExact()
	{
		Require("LootBonus", new Version(6, 0), LootBonusMvid);
		Require("FargowiltasSouls", new Version(1, 7, 3, 9), FargoSoulsMvid);
		Require("ImproveGame", new Version(1, 8, 2), ImproveGameMvid);
		Require("SOTS", new Version(0, 25, 1, 9), SotsMvid);
		Require("CalamityMod", new Version(2, 2, 2), CalamityMvid);
		Require("ThoriumMod", new Version(1, 7, 2, 8), ThoriumMvid);
		Require("PackBuilder", new Version(2, 0, 1), PackBuilderMvid);
		Require("RandomLoot", new Version(3, 2, 2), RandomLootMvid);
		Require("Fargowiltas", new Version(3, 3, 6, 7), FargoMutantMvid);
		Verified = true;
	}

	internal static Mod Require(string name, Version version, string mvid)
	{
		if (!ModLoader.TryGetMod(name, out Mod mod))
			throw new InvalidOperationException($"Hard-loot requires {name} in the exact66 profile.");
		Guid actual = mod.Code?.ManifestModule.ModuleVersionId ?? Guid.Empty;
		HardLootIdentityPolicy.Check(name, mod.Version.ToString(), actual,
			version.ToString(), mvid, message => mod.Logger.Warn(message));
		return mod;
	}

	internal static void Reset() => Verified = false;
}
