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
		if ((WorldGen.generatingWorld || WorldGen.gen) && ShouldPromoteLargeWorld())
			ApplyPresetIfLarge();
		if (WorldGen.generatingWorld || WorldGen.gen) AssertRequestedDimensions();
		bool creatingContinental = ContinentalWorldDimensions.IsContinental &&
			(WorldGen.generatingWorld || WorldGen.gen || continentalWorldRequested);
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
				ExitServerAfterMemoryDenial("create", denial);
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
			if (ShouldPromoteLargeWorld())
				ApplyPresetIfLarge();
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
		if (!Main.dedServ && Main.ActiveWorldFileData?.WorldSizeX == ContinentalWorldDimensions.Width &&
			Main.ActiveWorldFileData.WorldSizeY == ContinentalWorldDimensions.Height)
		{
			try { ContinentalWorldCapacity.AdmitClientLoad(); }
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

	private static void ApplyPresetIfLarge()
	{
		if (!ContinentalWorldDimensions.IsVanillaLarge)
			return;
		Main.maxTilesX = ContinentalWorldDimensions.Width;
		Main.maxTilesY = ContinentalWorldDimensions.Height;
		WorldGen.setWorldSize();
	}

	private static bool IsEnabled() =>
		ModContent.GetInstance<DingDongServerConfig>().EnableContinentalWorlds;

	private static void AssertRequestedDimensions()
	{
		string expected = System.Environment.GetEnvironmentVariable("DINGDONG_EXPECTED_WORLD_SIZE");
		if (!string.IsNullOrEmpty(expected) && expected != $"{Main.maxTilesX}x{Main.maxTilesY}")
			throw new System.InvalidOperationException($"Requested test dimensions {expected}, actual {Main.maxTilesX}x{Main.maxTilesY}; stopped before capacity allocation.");
	}

	private static bool ShouldPromoteLargeWorld() =>
		ContinentalCreationChoice.ShouldPromote(IsEnabled(), Main.dedServ,
			continentalWorldRequested,
			System.Environment.GetEnvironmentVariable("DINGDONG_SERVER_WORLD_SIZE"));
}
