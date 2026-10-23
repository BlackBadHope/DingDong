using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using MonoMod.RuntimeDetour;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static partial class SotsWorldgenAdapter
{
	private const string OresHash = "1FAA9EB9ACF7A5F58E04B09070E488DFDEE31535339F5AF19B36AF4B8DCC6EBA";
	private const string GeodesHash = "0FD87100090AD2526D8229E7AEDC946E4221FBA101CB4F2E3F28F55A9B82B4F8";
	private const string TopazHash = "2C7AFD8B9522D031FF22606D8ABD5CD94D608838962A9AB7F09DD37A6C2664B0";
	private const string SapphireHash = "AB6031640D4C15A10DB869654B0A7807EDE778AA00AF378F7120B9D9CE634F39";
	private const string GenerateTopazHash = "FD6CA2977F4D9C9AE120036380D3B21163436307F1D882613045789393D98D80";
	private const string GenerateSapphireHash = "5A97A1F7E28EBDA31E8154D1631BB38701831F0858D9E809DB3EF73DA0D389DF";
	private const string GenerateOreHash = "DCB6C0500259F9066591769C51EE0F39E0B181B0CFD9CC510DEE05066A1BD513";
	private const string FlattenEvilBiomeHash = "6489B054B1F7FFD32A150F20958E406DB9FF0F7D9A293A1420BFEDFE321C74FF";

	internal static void Install(Mod provider, List<IDisposable> hooks)
	{
		MethodInfo ores = InstancePass(provider, "GenSOTSOres");
		MethodInfo geodes = InstancePass(provider, "GenSOTSGeodes");
		MethodInfo topaz = StaticVoid(provider, "PlaceAndGenerateTopaz");
		MethodInfo sapphire = StaticVoid(provider, "PlaceAndGenerateSapphire");
		MethodInfo generateTopaz = StaticGenerator(provider, "GenerateTopazLihzahrdCamp");
		MethodInfo generateSapphire = StaticGenerator(provider, "GenerateSapphireIceCamp");
		MethodInfo generateOre = Exact66ProviderGate.Method(provider,
			"SOTS.WorldgenHelpers.SOTSWorldgenHelper", "GenerateFrigidIceOre", true,
			typeof(bool), typeof(int), typeof(int));
		MethodInfo pyramid = Exact66ProviderGate.Method(provider,
			"SOTS.WorldgenHelpers.PyramidWorldgenHelper", "GenerateSOTSPyramid", true,
			typeof(void), typeof(Mod), typeof(bool), typeof(int), typeof(int),
			typeof(int), typeof(int));
		MethodInfo flattenEvilBiome = Exact66ProviderGate.Method(provider,
			"SOTS.WorldgenHelpers.AbandonedVillageWorldgenHelper", "FlattenEvilBiome",
			true, typeof(void), typeof(int));
		MethodInfo outlineEvilBiome = Exact66ProviderGate.Method(provider,
			"SOTS.WorldgenHelpers.AbandonedVillageWorldgenHelper", "OutlineOneEvilBiome",
			true, typeof(bool));
		Exact66ProviderGate.AssertIl(ores, OresHash);
		Exact66ProviderGate.AssertIl(geodes, GeodesHash);
		Exact66ProviderGate.AssertIl(topaz, TopazHash);
		Exact66ProviderGate.AssertIl(sapphire, SapphireHash);
		Exact66ProviderGate.AssertIl(generateTopaz, GenerateTopazHash);
		Exact66ProviderGate.AssertIl(generateSapphire, GenerateSapphireHash);
		Exact66ProviderGate.AssertIl(generateOre, GenerateOreHash);
		Exact66ProviderGate.AssertIl(flattenEvilBiome, FlattenEvilBiomeHash);
		SotsGemPlacement.Bind(generateTopaz, generateSapphire);
		hooks.Add(new Exact66ActionDisposable(Reset));
		hooks.Add(new ILHook(ores, PatchOres));
		hooks.Add(new ILHook(geodes, PatchGeodes));
		hooks.Add(new ILHook(topaz, PatchTopaz));
		hooks.Add(new ILHook(sapphire, PatchSapphire));
		hooks.Add(new ILHook(pyramid, PatchPyramidWidthTable));
		hooks.Add(new ILHook(flattenEvilBiome, PatchFlattenEvilBiomeIndex));
		hooks.Add(new ILHook(outlineEvilBiome, PatchAbandonedVillageScan));
		InstallAbandonedMineGuard(provider, hooks);
		InstallAbandonedVillageCompatibility(provider, flattenEvilBiome, hooks);
		InstallVillageProtection(provider, hooks);
		InstallBigGeodeProtection(provider, hooks);
		InstallSanctuaryBounds(provider, hooks);
		InstallSanctuaryPlacement(provider, hooks);
	}

	private static MethodInfo InstancePass(Mod mod, string name) => Exact66ProviderGate.Method(mod,
		"SOTS.SOTSWorld", name, false, typeof(void), typeof(GenerationProgress), typeof(GameConfiguration));

	private static MethodInfo StaticVoid(Mod mod, string name) => Exact66ProviderGate.Method(mod,
		"SOTS.WorldgenHelpers.GemStructureWorldgenHelper", name, true, typeof(void));

	private static MethodInfo StaticGenerator(Mod mod, string name) => Exact66ProviderGate.Method(mod,
		"SOTS.WorldgenHelpers.GemStructureWorldgenHelper", name, true, typeof(void), typeof(int), typeof(int));

	private static void Reset()
	{
		SotsGemPlacement.Reset();
		ResetAbandonedMineGuard();
		ResetVillageProtection();
		SotsWorldgenReceipt.Reset();
	}
}
