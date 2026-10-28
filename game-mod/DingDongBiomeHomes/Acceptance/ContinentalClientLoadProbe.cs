using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using DingDongBiomeHomes.Worldgen;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.IO;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Acceptance;

// Explicit disposable profile only. Reads a runner-supplied world copy, never saves it.
public sealed class ContinentalClientLoadProbe : ModSystem
{
	private bool armed, started;
	public override void PostSetupContent()
	{
		armed = !Main.dedServ && Environment.GetEnvironmentVariable("DINGDONG_CLIENT_LOAD_PROBE") == "1";
		if (armed) Main.OnPostDraw += AfterDraw;
	}
	private void AfterDraw(GameTime time)
	{
		if (started || !Main.gameMenu || Main.menuMode != 0 || !NativeHeightCompatibilitySystem.Ready) return;
		started = true;
		_ = Task.Run(Run);
	}
	private void Run()
	{
		string output = Environment.GetEnvironmentVariable("DINGDONG_CLIENT_LOAD_OUTPUT");
		string path = Path.GetFullPath(Environment.GetEnvironmentVariable("DINGDONG_CLIENT_LOAD_WORLD") ?? "");
		string safeRoot = Path.GetFullPath(Environment.GetEnvironmentVariable("DINGDONG_CLIENT_LOAD_ROOT") ?? "");
		try
		{
			if (!path.StartsWith(safeRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
				!safeRoot.Contains("Fix-R32", StringComparison.OrdinalIgnoreCase) ||
				!Path.GetFullPath(output).StartsWith(safeRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
				throw new InvalidOperationException("Disposable Fix-R32 paths required.");
			long before = ContinentalMemoryStatus.Read().AvailableCommitBytes;
			ContinentalWorldCapacity.AdmitClientLoad(ContinentalWorldDimensions.X10); // Presets 28.10: x10 fixture
			long after = ContinentalMemoryStatus.Read().AvailableCommitBytes;
			Main.ActiveWorldFileData = WorldFile.GetAllMetadata(path, false);
			WorldFile.LoadWorld(false);
			if (WorldGen.loadFailed || !WorldGen.loadSuccess)
				throw new InvalidOperationException("Native world load failed.", WorldFile.LastThrownLoadException);
			if (Main.maxTilesX != 28000 || Main.maxTilesY != 7200 || Main.tile.Width != 28001 || Main.Map == null)
				throw new InvalidOperationException("Native capacity/map incomplete after load.");
			File.WriteAllText(output, JsonSerializer.Serialize(new { status = "PASS", nativeClient = true,
				width = Main.maxTilesX, height = Main.maxTilesY, availableCommitBefore = before,
				availableCommitAfterCollection = after, availableCommitAfterLoad = ContinentalMemoryStatus.Read().AvailableCommitBytes,
				version = Mod.Version.ToString(), saved = false, mapReady = Main.Map != null }));
		}
		catch (Exception ex)
		{
			Mod.Logger.Error("DINGDONG_CLIENT_LOAD_PROBE|verdict=FAIL", ex);
			File.WriteAllText(output, JsonSerializer.Serialize(new { status = "FAIL", error = ex.ToString(), saved = false }));
		}
		finally { Main.QueueMainThreadAction(() => Main.instance.Exit()); }
	}
	public override void Unload()
	{
		if (armed) Main.OnPostDraw -= AfterDraw;
		armed = started = false;
	}
}
