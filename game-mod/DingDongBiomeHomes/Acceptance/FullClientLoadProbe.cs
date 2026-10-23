using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DingDongBiomeHomes.Compatibility;
using DingDongBiomeHomes.Worldgen;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Acceptance;

// Only armed by the disposable test runner; never selects a player or world.
public sealed class FullClientLoadProbe : ModSystem
{
	private bool armed, complete;
	public override void PostSetupContent()
	{
		if (Main.dedServ || Environment.GetEnvironmentVariable("DINGDONG_FULL_CLIENT_PROBE") != "1") return;
		armed = true;
		Main.OnPostDraw += AfterDraw;
	}
	private void AfterDraw(GameTime time)
    {
        if (!armed || complete) return;
        try { AfterDrawCore(time); }
        catch (Exception ex) {
            complete = true;
            string output = Environment.GetEnvironmentVariable("DINGDONG_FULL_CLIENT_PROBE_OUTPUT");
            File.WriteAllText(output, JsonSerializer.Serialize(new { status = "FAIL", error = ex.ToString(), worldOpened = false, playerOpened = false }));
            Main.instance.Exit();
        }
    }
    private void AfterDrawCore(GameTime time)
	{
		if (!armed || complete || !Main.gameMenu || Main.menuMode != 0 || !NativeHeightCompatibilitySystem.Ready) return;
		string enabledPath = Environment.GetEnvironmentVariable("DINGDONG_PROBE_ENABLED_PATH");
		string output = Environment.GetEnvironmentVariable("DINGDONG_FULL_CLIENT_PROBE_OUTPUT");
		if (string.IsNullOrEmpty(enabledPath) || string.IsNullOrEmpty(output)) throw new InvalidOperationException("Explicit client probe paths required.");
		string[] expected = JsonSerializer.Deserialize<string[]>(File.ReadAllText(enabledPath));
		string[] missing = expected.Where(name => !ModLoader.TryGetMod(name, out _)).ToArray();
		if (expected.Length != 66 || missing.Length != 0) throw new InvalidOperationException("Client probe missing mods: " + string.Join(",", missing));
		int uiChecks = LansUncraftCompatibility.RunRuntimeChecks() + UncraftRecipeCompatibility.CheckNativeClientPanel();
		int inventoryControlsChecks = Diagnostics.InventoryControlsRuntimeProbe.Run();
		complete = true;
		File.WriteAllText(output, JsonSerializer.Serialize(new { status = "PASS", version = Mod.Version.ToString(),
			nativeClient = true, enabledMods = expected.Length, allEnabledModsLoaded = true, heightCompatibilityReady = true,
			uiChecks, inventoryControlsChecks, menuMode = Main.menuMode, menuFrameDrawn = true, worldOpened = false, playerOpened = false,
			ownMvid = GetType().Module.ModuleVersionId, loadedMods = ModLoader.Mods.Select(m => m.Name).ToArray() }));
		Main.instance.Exit();
	}
	public override void Unload()
	{
		if (armed) Main.OnPostDraw -= AfterDraw;
		armed = complete = false;
	}
}
