using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
/// <summary>
/// Reads difficulty state for explanation only. External difficulty mods remain
/// the sole owners of their scaling and AI hooks; DingDong never applies these
/// values a second time.
/// </summary>
public sealed class DamageDifficultyContextSystem : ModSystem
{
	private static readonly List<IOptionalDifficultyAdapter> Adapters = new();
	private static bool initialized;
	private static bool worldLoaded;
	internal static bool IsWorldReady => worldLoaded || (!Main.gameMenu && Main.ActiveWorldFileData != null);

	internal static int AdapterCount
	{
		get
		{
			EnsureInitialized();
			return Adapters.Count;
		}
	}

	public override void PostSetupContent() => EnsureInitialized();
	public override void OnWorldLoad() => worldLoaded = false;
	public override void PostWorldLoad() => worldLoaded = true;
	public override void OnWorldUnload() => worldLoaded = false;
	public override void Unload()
	{
		Adapters.Clear();
		initialized = false;
		worldLoaded = false;
	}

	private static void EnsureInitialized()
	{
		if (initialized)
			return;
		initialized = true;
		Adapters.Clear();
		if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
			AddAdapter("Calamity", () => new CalamityDifficultyAdapter(calamity));
		if (ModLoader.TryGetMod("FargowiltasSouls", out Mod fargoSouls))
			AddAdapter("Fargo's Souls", () => new FargoDifficultyAdapter(fargoSouls));
	}

	internal static DamageDifficultySnapshot Capture()
	{
		EnsureInitialized();
		List<DamageDifficultyEntry> external = new();
		bool worldReady = IsWorldReady;
		foreach (IOptionalDifficultyAdapter adapter in Adapters)
			external.Add(adapter.Read(worldReady));
		return new DamageDifficultySnapshot(VanillaModeName(), SeedContextName(), DamageAuthoritySystem.IsEnabled, external.AsReadOnly());
	}

	private static string VanillaModeName()
	{
		if (Main.GameModeInfo.IsJourneyMode)
			return Text("Journey");
		if (Main.masterMode && Main.getGoodWorld)
			return Text("Legendary");
		if (Main.masterMode)
			return Text("Master");
		if (Main.expertMode)
			return Text("Expert");
		return Text("Classic");
	}

	private static string SeedContextName() => Main.getGoodWorld ? Text("ForTheWorthy") : Text("StandardSeed");
	private static string Text(string key) => Language.GetTextValue($"Mods.DingDongBiomeHomes.Damage.Guide.Difficulty.{key}");
	internal static void MarkWorldReady() => worldLoaded = true;
	private static void AddAdapter(string source, Func<IOptionalDifficultyAdapter> factory)
	{
		try
		{
			Adapters.Add(factory());
		}
		catch (Exception exception)
		{
			Adapters.Add(new FailedDifficultyAdapter(source, exception));
		}
	}
}
