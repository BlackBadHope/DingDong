using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

public static partial class DingDongAuthoredStructureLoader
{
	internal static IDisposable TraceCloudFraming(Mod mod, AuthoredPreset preset, Point origin)
	{
		if (Environment.GetEnvironmentVariable("DINGDONG_HEIGHT_COMPAT_PROBE") != "1" ||
			preset.Id != "Huadiao_FireSkyVillage") return null;
		return new CloudTrace(mod, new Rectangle(origin.X, origin.Y, preset.Width, preset.Height));
	}
	private sealed class CloudTrace : IDisposable
	{
		private readonly Mod mod; private readonly Rectangle area; private int count;
		internal CloudTrace(Mod mod, Rectangle area) { this.mod = mod; this.area = area; On_WorldGen.KillTile += Kill; }
		private void Kill(On_WorldGen.orig_KillTile next, int x, int y, bool fail, bool effectOnly, bool noItem)
		{
			Tile tile = Main.tile[x, y];
			if (area.Contains(x, y) && tile.HasTile && tile.TileType == TileID.Cloud && count++ < 32)
			{
				string folder = Path.Combine(Main.SavePath, "DingDongDiagnostics"); Directory.CreateDirectory(folder);
				File.AppendAllText(Path.Combine(folder, "cloud-kills.jsonl"), JsonSerializer.Serialize(new {
					x, y, fail, effectOnly, noItem, liquid = tile.LiquidAmount, kind = tile.LiquidType,
					WorldGen.gen, WorldGen.generatingWorld, Main.getGoodWorld, Liquid.quickSettle,
					stack = new StackTrace(true).ToString() }) + Environment.NewLine);
			}
			next(x, y, fail, effectOnly, noItem);
		}
		public void Dispose() => On_WorldGen.KillTile -= Kill;
	}
	// Caller admits only the opt-in isolated world fixture. A scoped replay
	// identifies the writer before changing import or integrity semantics.
	internal static object ProbeCloudFraming(Mod mod)
	{
		NpcAuthoredPresetManifest.TryGetCandidate("CalamityMod/BrimstoneWitch", out AuthoredPreset preset);
		AuthoredCandidateDeploymentManifest.TryGet("CalamityMod/BrimstoneWitch", out AuthoredDeploymentProfile profile);
		StructureData data = GetOrLoad(mod, preset.ResourcePath);
		Point origin = new(12579, Worldgen.NativeUnderworldSystem.Layer + 32);
		Rectangle area = new(origin.X - 12, origin.Y - 12, data.Width + 24, data.Height + 24);
		var saved = new AuthoredTileSnapshot[area.Width * area.Height];
		for (int x = 0; x < area.Width; x++) for (int y = 0; y < area.Height; y++)
			saved[x * area.Height + y] = AuthoredTileSnapshot.Read(Main.tile[area.X + x, area.Y + y]);
		var kills = new List<object>();
		void Kill(On_WorldGen.orig_KillTile next, int x, int y, bool fail, bool effectOnly, bool noItem)
		{
			Tile tile = Main.tile[x, y];
			if (area.Contains(x, y) && tile.HasTile && tile.TileType == TileID.Cloud && kills.Count < 32)
				kills.Add(new { x, y, fail, effectOnly, noItem, liquid = tile.LiquidAmount, kind = tile.LiquidType, stack = new StackTrace().ToString() });
			next(x, y, fail, effectOnly, noItem);
		}
		bool gen = WorldGen.gen;
		On_WorldGen.KillTile += Kill;
		try
		{
			WorldGen.gen = true;
			Generate(mod, preset, origin, profile);
			var beforeHousing = InspectIntegrity(mod, preset, origin, profile);
			HousingProbeResult housing = AuthoredSettlementBuilder.ProbeHousing(new Rectangle(origin.X, origin.Y, data.Width, data.Height));
			var afterHousing = InspectIntegrity(mod, preset, origin, profile);
			return new { verdict = afterHousing.Passed ? "PASS" : "FAIL", origin, beforeHousing, afterHousing,
				housing.ValidRooms, cloudLavaDeath = Main.tileLavaDeath[TileID.Cloud], kills,
				fixtureTilesRestored = true, originalWorldUsed = false };
		}
		finally
		{
			On_WorldGen.KillTile -= Kill; WorldGen.gen = gen;
			for (int x = 0; x < area.Width; x++) for (int y = 0; y < area.Height; y++)
				saved[x * area.Height + y].Restore(Main.tile[area.X + x, area.Y + y]);
		}
	}
}
