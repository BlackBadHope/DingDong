using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using MonoMod.RuntimeDetour;
using Terraria.ModLoader;
using DingDongBiomeHomes.Worldgen.Compiler.Runtime;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

public sealed partial class NativeSotsGemSearchSystem : ModSystem
{
	private delegate void Original();
	private readonly List<Hook> hooks = new();
	private Action<int, int> amber, amethyst;
	private Func<int, int, bool, bool> solid;
	private ushort pyramid;
	private bool calamity;

	public override void PostSetupContent()
	{
		if (!ModLoader.TryGetMod("SOTS", out Mod provider)) return;
		try
		{
			amber = Generator(provider, "GenerateAmberWaterVault");
			amethyst = Generator(provider, "GenerateAmethystDesertCamp");
			solid = Exact66ProviderGate.Method(provider,
				"SOTS.WorldgenHelpers.SOTSWorldgenHelper", "TrueTileSolid",
				true, typeof(bool), typeof(int), typeof(int), typeof(bool))
				.CreateDelegate<Func<int, int, bool, bool>>();
			pyramid = provider.Find<ModTile>("PyramidSlabTile").Type;
			calamity = ModLoader.HasMod("CalamityMod");
			Install(provider, "Amber", PlaceAmber);
			Install(provider, "Amethyst", PlaceAmethyst);
			foreach (string gem in new[] { "Diamond", "Topaz", "Sapphire" })
			{
				string name = gem;
				Install(provider, name, original => Trace(name, original));
			}
			Mod.Logger.Info("DINGDONG_NATIVE_GEM_SEARCH|installed=True|boundedAmber=True|cachedAmethyst=True");
		}
		catch { Unload(); throw; }
	}

	private static bool Active => ContinentalWorldDimensions.IsContinental &&
		WorldCompilerRuntimeState.Current == null;

	private static Action<int, int> Generator(Mod mod, string name) =>
		Exact66ProviderGate.Method(mod, "SOTS.WorldgenHelpers.GemStructureWorldgenHelper",
			name, true, typeof(void), typeof(int), typeof(int))
		.CreateDelegate<Action<int, int>>();

	private void Install(Mod mod, string name, Action<Original> replacement)
	{
		MethodInfo target = Exact66ProviderGate.Method(mod,
			"SOTS.WorldgenHelpers.GemStructureWorldgenHelper",
			"PlaceAndGenerate" + name, true, typeof(void));
		hooks.Add(new Hook(target, replacement));
	}

	private void Trace(string name, Original original)
	{
		if (!Active) { original(); return; }
		var clock = Stopwatch.StartNew();
		Mod.Logger.Info($"DINGDONG_NATIVE_GEM_START|gem={name}");
		original();
		Mod.Logger.Info($"DINGDONG_NATIVE_GEM_END|gem={name}|elapsedMs={clock.ElapsedMilliseconds}");
	}

	public override void Unload()
	{
		foreach (Hook hook in hooks) hook.Dispose();
		hooks.Clear();
		amber = amethyst = null;
		solid = null;
	}
}
