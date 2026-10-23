using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen;

// Test-only reservation provenance. No tile snapshots, edits or retained world map.
internal static class NativeReservationProbe
{
	private static readonly Dictionary<Rectangle, string> stages = new();
	private static bool Enabled => Main.dedServ &&
		Environment.GetEnvironmentVariable("DINGDONG_HEIGHT_COMPAT_PROBE") == "1";
	internal static void AfterPass(string name)
	{
		if (!Enabled) return;
		if (name == "Reset") stages.Clear();
		foreach (Rectangle region in ForeignStructureIntegrityGuard.ReadReservations())
			if (stages.Count < 16384) stages.TryAdd(region, name);
	}
	internal static void Sea(Mod owner, bool left, int width, int top, int bottom)
	{
		if (!Enabled || !Path.GetFileName(Main.ActiveWorldFileData.Path)
			.StartsWith("DingDongTerrainProbe-", StringComparison.Ordinal)) return;
		Rectangle coast = new(left ? 0 : Main.maxTilesX - width - 400, top - 40, width + 400, bottom - top + 80);
		var regions = ForeignStructureIntegrityGuard.ReadReservations().Where(r => r.Intersects(coast))
			.Select(r => new { r.X, r.Y, r.Width, r.Height, stage = stages.GetValueOrDefault(r, "untracked") }).ToArray();
		string folder = Path.Combine(Main.SavePath, "DingDongDiagnostics", "SeaReservations");
		Directory.CreateDirectory(folder);
		string path = Path.Combine(folder, Path.GetFileNameWithoutExtension(Main.ActiveWorldFileData.Path) + ".json");
		File.WriteAllText(path, JsonSerializer.Serialize(new { left, width, top, bottom, regions }, new JsonSerializerOptions { WriteIndented = true }));
		owner.Logger.Info($"DINGDONG_SEA_RESERVATIONS|count={regions.Length}|path={path}|tileEdits=False");
	}
}
