using System;
using System.Collections.Generic;
using Terraria.ModLoader;
using global::DingDongBiomeHomes.Worldgen.Compiler.Runtime;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static class Exact66WorldgenAdapters
{
	private static readonly List<IDisposable> Hooks = [];
	private const int RuntimeHookCount = 33 + 7 + 6; // Village/geode protections, mushroom roots and Sanctuary height callers.
	private const int CleanupCount = 4;
	private const int FingerprintCount = 17 + 10; // Village admission/footprints and big-geode template.
	private static bool installed;
	internal static bool Ready => installed && Hooks.Count == RuntimeHookCount + CleanupCount;
	// Presets 28.10: "<id>-<W>x<H>" of the current preset; ram32 stays exactly "x10-28000x7200".
	internal static string Receipt => "exact66:SOTS-0.25.1.9;ThoriumMod-1.7.2.8;CalamityMod-2.2.2;" +
		(ContinentalWorldDimensions.Current ?? ContinentalWorldDimensions.X10).ReceiptTag;
	internal static bool WorldgenComplete =>
		Ready && ContinentalWorldgenScaleReceipt.Complete &&
		SotsWorldgenReceipt.Complete && ThoriumWorldgenReceipt.Complete &&
		CalamityWorldgenReceipt.Complete;
	internal static string WorldgenReceipt =>
		$"{ContinentalWorldgenScaleReceipt.Value};{SotsWorldgenReceipt.Value};" +
		$"{ThoriumWorldgenReceipt.Value};{CalamityWorldgenReceipt.Value}";
	internal static bool CompilerWorldgenComplete(
		WorldCompilerExecutionReceipt compiler) => Ready && compiler != null &&
		compiler.Complete && compiler.Compatibility?.Complete == true &&
		compiler.Equivalents.Complete && ContinentalWorldgenScaleReceipt.Complete &&
		SotsWorldgenReceipt.CompilerComplete && ThoriumWorldgenReceipt.Complete &&
		CalamityWorldgenReceipt.CompilerComplete(compiler.Equivalents);
	internal static string CompilerWorldgenReceipt(
		WorldCompilerExecutionReceipt compiler) => WorldgenReceipt +
		$";compilerCompatibility={compiler?.Compatibility?.Completed ?? 0}/" +
		$"{compiler?.Compatibility?.Expected ?? 0};compilerEquivalents=" +
		(compiler == null ? "missing" : compiler.Equivalents.Value);

	internal static void ResetWorldgenState()
	{
		SotsWorldgenReceipt.Reset();
		ThoriumWorldgenReceipt.Reset();
		CalamityWorldgenReceipt.Reset();
		ContinentalWorldgenScaleReceipt.Reset();
	}

	internal static void PrepareWorldgen()
	{
		ResetWorldgenState();
		ContinentalVanillaBufferScaler.Ensure();
		CalamityAbyssBufferScaler.Ensure();
	}

	internal static void Install(Mod owner)
	{
		if (installed)
			return;
		try
		{
			Mod sots = Exact66ProviderGate.Require("SOTS", new Version(0, 25, 1, 9),
				"14c19a4a-afb8-49fb-ab76-cd75dff954ac");
			Mod thorium = Exact66ProviderGate.Require("ThoriumMod", new Version(1, 7, 2, 8),
				"c3026632-3918-4387-be6d-aa5539117e51");
			Mod calamity = Exact66ProviderGate.Require("CalamityMod", new Version(2, 2, 2),
				"0e63c698-ec72-446e-ac1f-8d01f0ee7ac7");
			SotsWorldgenAdapter.Install(sots, Hooks);
			ThoriumWorldgenAdapter.Install(thorium, Hooks);
			CalamityWorldgenAdapter.Install(calamity, Hooks);
			installed = true;
			if (!Ready) throw new InvalidOperationException($"Exact66 hook receipt changed: {Hooks.Count}/{RuntimeHookCount + CleanupCount}.");
			owner.Logger.Info($"DINGDONG_EXACT66_WORLDGEN_ADAPTERS|verdict=PASS|receipt={Receipt}|runtimeHooks={RuntimeHookCount}|cleanup={CleanupCount}|fingerprints={FingerprintCount}|replayedPasses=0");
		}
		catch (Exception failure)
		{
			Rollback(failure);
			throw;
		}
	}

	internal static void Uninstall()
	{
		List<Exception> failures = [];
		for (int i = Hooks.Count - 1; i >= 0; i--)
			try { Hooks[i].Dispose(); }
			catch (Exception error) { failures.Add(error); }
		Hooks.Clear();
		installed = false;
		ResetWorldgenState();
		if (failures.Count != 0)
			throw new AggregateException("Exact66 worldgen adapter cleanup failed.", failures);
	}

	private static void Rollback(Exception original)
	{
		try { Uninstall(); }
		catch (Exception cleanup)
		{
			throw new AggregateException("Exact66 worldgen adapter admission and rollback failed.", original, cleanup);
		}
	}
}
