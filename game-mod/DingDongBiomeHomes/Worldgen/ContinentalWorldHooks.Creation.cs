using Terraria;
using Terraria.ModLoader;
using Terraria.WorldBuilding;
using Terraria.Localization;

namespace DingDongBiomeHomes.Worldgen;

internal static partial class ContinentalWorldHooks
{
	private static void ClearWorld(On_WorldGen.orig_clearWorld orig)
	{
		int previousWidth = Main.maxTilesX;
		int previousHeight = Main.maxTilesY;
		// Promotion belongs only to new-world generation. Capacity alignment does
		// not: LoadHeader sets maxTiles before clearWorld invokes mod callbacks.
		// Presets 28.10: promote the carrier to the requested preset instead of fixed x10.
		ContinentalWorldPreset requested = WorldGen.generatingWorld || WorldGen.gen ? RequestedPreset() : null;
		if (requested != null)
			ApplyPreset(requested);
		if (WorldGen.generatingWorld || WorldGen.gen) AssertRequestedDimensions();
		bool creatingContinental = ContinentalWorldDimensions.IsContinental &&
			(WorldGen.generatingWorld || WorldGen.gen || requestedPreset != null);
		try
		{
			ContinentalWorldCapacity.Ensure("clear-world-pre-callbacks",
				deferClientMap: creatingContinental, generation: creatingContinental);
		}
		catch (ContinentalMemoryAdmissionException denial)
		{
			bool serverCreation = Main.dedServ && (WorldGen.generatingWorld || WorldGen.gen);
			// Commit can change after the menu check. Admission runs before
			// ResizeCore: no tiles or mod callbacks have been changed yet.
			Main.maxTilesX = previousWidth;
			Main.maxTilesY = previousHeight;
			WorldGen.setWorldSize();
			WorldGen.generatingWorld = false;
			WorldGen.gen = false;
			ClearWorldCreationRequest();
			// RamOpt 28.10: a dedicated server would otherwise fall through to the
			// failed-load branch of Main.DedServ_PostModLoad and block on Console.ReadKey.
			if (serverCreation)
				ExitServerAfterMemoryDenial("create", denial, requested);
			throw; // Native worldgen error menu, not the fatal OOM handler.
		}
		// A genuine world clear starts a new section epoch even when the next
		// world has the same dimensions. Vanilla recreates sectionManager.
		ContinentalWorldCapacity.ResetRemoteSectionState();
		ContinentalSectionStreamingNetwork.Reset();
		orig();
	}

	private static void GenerateWorld(
		On_WorldGen.orig_GenerateWorld orig,
		int seed,
		GenerationProgress progress)
	{
		try
		{
			if (RequestedPreset() is ContinentalWorldPreset preset)
				ApplyPreset(preset);
			AssertRequestedDimensions();
			try
			{
				ContinentalWorldCapacity.Ensure("worldgen-entry",
					deferClientMap: ContinentalWorldDimensions.IsContinental, generation: true);
			}
			// RamOpt 28.10: measured on the dedicated server (RamCheck-failfast attempt 1):
			// -autocreate generates from WorldFile.LoadWorld -> GenerateWorld, after a
			// clearWorld that ran before promotion, so the refusal is raised here. Without
			// this the server reaches "if (!autoShutdown) Console.ReadKey()" and blocks.
			catch (ContinentalMemoryAdmissionException denial) when (Main.dedServ)
			{
				ExitServerAfterMemoryDenial("create", denial);
				throw;
			}
			using var throughput = new WorldgenThroughputScope(
				ContinentalWorldDimensions.IsContinental,
				message => DingDongBiomeHomes.Instance.Logger.Warn(
					"Optional worldgen GC mode unavailable: " + message));
			try
			{
				orig(seed, progress);
			}
			// RamOpt review fix 28.10: if commit still runs out mid-generation (the gate
			// keeps a margin, but the true in-pass peak is not measured), a dedicated
			// server exits at once with the memory exit code instead of the fatal handler.
			// tML's WorldGenerator rethrows the original exception (bare "throw;").
			catch (System.OutOfMemoryException oom) when (Main.dedServ && ContinentalWorldDimensions.IsContinental)
			{
				ExitServerAfterGenerationOutOfMemory(oom);
				throw;
			}
		}
		finally
		{
			ClearWorldCreationRequest();
		}
	}

	private static void PlayWorldCallback(
		On_WorldGen.orig_playWorldCallBack orig, object threadContext)
	{
		// The previous attempt's dimensions can still be active in the menu.
		// Do not allocate a map here: the selected header/clearWorld owns it.
		// Presets 28.10: any continental preset, admitted with its own storage.
		if (!Main.dedServ && Main.ActiveWorldFileData is Terraria.IO.WorldFileData data &&
			ContinentalWorldDimensions.TryResolve(data.WorldSizeX, data.WorldSizeY) is ContinentalWorldPreset loading)
		{
			try { ContinentalWorldCapacity.AdmitClientLoad(loading); }
			catch (ContinentalMemoryAdmissionException ex)
			{
				ShowLoadMemoryError(ex);
				return;
			}
		}
		orig(threadContext);
		if (!Main.dedServ && WorldGen.loadFailed &&
			Terraria.IO.WorldFile.LastThrownLoadException is ContinentalMemoryAdmissionException memoryError)
			ShowLoadMemoryError(memoryError);
	}

	private static void ShowLoadMemoryError(ContinentalMemoryAdmissionException error)
	{
		DingDongBiomeHomes.Instance.Logger.Warn("DINGDONG_CONTINENTAL_MEMORY|verdict=DENIED|scope=load-menu|worldFileUnchanged=True|reason=" + error.Message);
		Main.QueueMainThreadAction(() => Utils.ShowFancyErrorMessage(
			Language.GetTextValue($"{MenuKey}.LoadMemoryDenied", error.Message), 6));
	}

	private static bool UpdateMapTile(On_WorldGen.orig_UpdateMapTile orig,
		int i, int j, bool addToList) =>
		ContinentalWorldCapacity.ClientMapDeferred ? false :
			orig(i, j, addToList);

	// Presets 28.10: vanilla Large is the carrier set by the client menu; a dedicated server also
	// promotes -autocreate 1/2 (Small/Medium) to the chosen preset (owner decision B4).
	// Presets 28.10 review fix: the client accepts any vanilla size too. Vanilla FinishCreatingWorld
	// runs ProcessSeed first, and a four-part seed "size.difficulty.evil.seed" (e.g. "1.2.1.123456",
	// the copy-seed form) rewrites _optionSize to Small/Medium after our Large carrier was set; the
	// preset was already chosen and memory-admitted, so it must still be applied. ApplyPreset only
	// runs with a non-null preset (menu choice or server request), never for a plain vanilla world.
	private static void ApplyPreset(ContinentalWorldPreset preset)
	{
		bool carrier = ContinentalWorldDimensions.IsVanillaSize(Main.maxTilesX, Main.maxTilesY);
		if (!carrier)
			return;
		Main.maxTilesX = preset.Width;
		Main.maxTilesY = preset.Height;
		WorldGen.setWorldSize();
	}

	// Presets 28.10: true unless the hidden "restore vanilla sizes" switch is on (B3).
	private static bool IsEnabled() =>
		ModContent.GetInstance<DingDongServerConfig>().EnableContinentalWorlds;

	private static void AssertRequestedDimensions()
	{
		string expected = System.Environment.GetEnvironmentVariable("DINGDONG_EXPECTED_WORLD_SIZE");
		if (!string.IsNullOrEmpty(expected) && expected != $"{Main.maxTilesX}x{Main.maxTilesY}")
			throw new System.InvalidOperationException($"Requested test dimensions {expected}, actual {Main.maxTilesX}x{Main.maxTilesY}; stopped before capacity allocation.");
	}

	// Presets 28.10: preset requested for the world being created, or null (no promotion).
	private static ContinentalWorldPreset RequestedPreset()
	{
		string serverSize = System.Environment.GetEnvironmentVariable(ContinentalCreationChoice.ServerSizeVariable);
		long? physical = Main.dedServ ? TotalPhysicalBytesOrNull() : null;
		ContinentalWorldPreset preset = ContinentalCreationChoice.ResolveRequestedPreset(IsEnabled(), Main.dedServ,
			requestedPreset, serverSize, physical);
		// Presets 28.10 review fix: say once why a default server stays vanilla.
		if (Main.dedServ && preset == null && string.IsNullOrWhiteSpace(serverSize) && IsEnabled() && !serverDefaultLogged)
		{
			serverDefaultLogged = true;
			DingDongBiomeHomes.Instance?.Logger.Info(
				$"DINGDONG_SERVER_WORLD_SIZE|value=empty|promotion=none|reason={(physical is null ? "physical-ram-unknown" : "no-available-preset-fits-physical-ram")}" +
				$"|physical={(physical?.ToString() ?? "unknown")}|available={string.Join(",", System.Linq.Enumerable.Select(ContinentalWorldDimensions.Available, p => p.Id))}");
		}
		return preset;
	}

	private static bool serverDefaultLogged;

	private static long? TotalPhysicalBytesOrNull()
	{
		try { return ContinentalMemoryStatus.Read().TotalPhysicalBytes; }
		catch (System.Exception ex) when (ex is System.PlatformNotSupportedException or
			System.ComponentModel.Win32Exception or ContinentalMemoryAdmissionException)
		{
			return null;
		}
	}
}
