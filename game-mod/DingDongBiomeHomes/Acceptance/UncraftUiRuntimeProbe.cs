using System;
using System.IO;
using System.Text.Json;
using DingDongBiomeHomes.Compatibility;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Acceptance;

public sealed class UncraftUiRuntimeProbe : ModSystem
{
	public override void PostSetupContent()
	{
		if (Main.dedServ || Environment.GetEnvironmentVariable("DINGDONG_UI_LAYOUT_PROBE") != "1") return;
		string path = Environment.GetEnvironmentVariable("DINGDONG_UI_LAYOUT_PROBE_OUTPUT");
		if (string.IsNullOrWhiteSpace(path)) throw new InvalidOperationException("Explicit UI probe output path required.");
		int checks = LansUncraftCompatibility.RunRuntimeChecks();
		File.WriteAllText(path, JsonSerializer.Serialize(new { status = "PASS", nativeClient = true, checks,
			version = Mod.Version.ToString(), worldOpened = false, playerOpened = false,
			provider = "LansUncraftItems2023.10.8.1", autoUncraftSlot = "40x40", screenshotsInspected = false }));
		if (Environment.GetEnvironmentVariable("DINGDONG_FULL_CLIENT_PROBE") != "1")
			Main.QueueMainThreadAction(() => Main.instance.Exit());
	}
}
