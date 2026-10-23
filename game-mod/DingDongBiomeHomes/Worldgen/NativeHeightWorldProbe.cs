using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen;

// Read-only evidence from an actually loaded, isolated generated world.
internal sealed class NativeHeightWorldProbe : ModSystem
{
	private System.Threading.Timer exitTimer;
	private int exitRequested;
	private static readonly FieldInfo ServerThread = typeof(Netplay).GetField("_serverThread", BindingFlags.Static | BindingFlags.NonPublic);
	public override void OnWorldUnload() { exitTimer?.Dispose(); exitTimer = null; }
	public override void PostWorldLoad()
	{
		string world = Main.ActiveWorldFileData?.Path;
		if (!Main.dedServ || Environment.GetEnvironmentVariable("DINGDONG_HEIGHT_COMPAT_PROBE") != "1" ||
			world == null || !Path.GetFileName(world).StartsWith("DingDongTerrainProbe-", StringComparison.Ordinal)) return;
		try { Capture(world); }
		catch (Exception ex)
		{
			// A failed isolated assertion is evidence, not a load exception. Let the
			// native test server exit normally instead of entering its ReadKey UI.
			string folder = Path.Combine(Main.SavePath, "DingDongDiagnostics", "HeightWorld");
			Directory.CreateDirectory(folder);
			File.WriteAllText(Path.Combine(folder, Path.GetFileNameWithoutExtension(world) + ".json"),
				JsonSerializer.Serialize(new { verdict = "FAIL", world, error = ex.ToString(),
					visualAcceptance = false, liveMultiplayerAcceptance = false }));
			Mod.Logger.Error("DINGDONG_HEIGHT_WORLD_PROBE|verdict=FAIL|fixtureOnly=True|error=" + ex.Message);
		}
		finally { ScheduleExit(world); }
	}

	private void Capture(string world)
	{
		int checks = 0;
		void Require(bool pass, string name) { if (!pass) throw new InvalidOperationException("Height world probe: " + name); checks++; }
		Require(ContinentalWorldDimensions.IsContinental, "fixture dimensions");
		Require(NativeUnderworldSystem.GeometryVersion > 0, "persisted expanded Underworld version");
		int layer = NativeUnderworldSystem.Layer;
		int reflectedLayer = (int)typeof(Main).GetProperty(nameof(Main.UnderworldLayer)).GetValue(null);
		Require(layer == reflectedLayer, "native getter and saved geometry disagree");
		Type closure = typeof(Terraria.ModLoader.Utilities.SpawnCondition).GetNestedType("<>c", BindingFlags.NonPublic);
		MethodInfo predicate = closure.GetMethod("<.cctor>b__112_94", BindingFlags.Instance | BindingFlags.NonPublic);
		object receiver = closure.GetField("<>9", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
		bool Hell(int y) => (bool)predicate.Invoke(receiver, new object[] { new NPCSpawnInfo { SpawnTileY = y } });
		Require(!Hell(layer + 10) && Hell(layer + 11), "actual native Hell spawn boundary");
		if (!NativeCoastalBiomeSystem.Ready) ModContent.GetInstance<NativeCoastalBiomeSystem>().PostWorldLoad();
		Require(NativeCoastalBiomeSystem.Ready, "loaded coast bounds missing");
		var left = NativeCoastalBiomeSystem.Left; var right = NativeCoastalBiomeSystem.Right;
		Require(left.Width > 0 && right.Width > 0, "both generated coasts required");
		Require(NativeCoastalBiomeSystem.IsOcean(left.Width - 2, left.Bottom - 1), "left deep ocean biome");
		Require(NativeCoastalBiomeSystem.IsOcean(Main.maxTilesX - right.Width + 1, right.Bottom - 1), "right deep ocean biome");
		int sulphurWidth = 0;
		if (ModLoader.TryGetMod("CalamityMod", out Mod calamity) && !Main.remixWorld)
		{
			Type sea = calamity.Code.GetType("CalamityMod.World.SulphurousSea", true);
			sulphurWidth = (int)sea.GetProperty("BiomeWidth").GetValue(null);
			bool atLeft = (bool)calamity.Code.GetType("CalamityMod.World.Abyss", true).GetField("AtLeftSideOfWorld").GetValue(null);
			var gate = calamity.Code.GetType("CalamityMod.BiomeManagers.SulphurousSeaBiome", true)
				.GetMethod("IsInBiomePosition", new[] { typeof(Point) }).CreateDelegate<Func<Point, bool>>();
			int inner = atLeft ? sulphurWidth - 1 : Main.maxTilesX - sulphurWidth + 1;
			int outer = atLeft ? sulphurWidth + 1 : Main.maxTilesX - sulphurWidth - 1;
			Require(gate(new Point(inner, 50)) && !gate(new Point(outer, 50)), "actual loaded sulphur biome width");
		}
		var bands = new List<object>(); int totalOpen = 0;
		foreach (int offset in new[] { 20, 80, 160 })
		{
			int y = layer + offset, samples = 0, open = 0;
			for (int x = 1000; x < Main.maxTilesX - 1000; x += 128)
			{
				Tile tile = Main.tile[x, y]; samples++;
				if (!tile.HasTile || !Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]) open++;
			}
			totalOpen += open; bands.Add(new { y, samples, open });
		}
		Require(totalOpen > 0, "expanded Hell has no sampled open terrain");
		object settlement = ProbeSettlement();
		object wallOfFleshGates = ProbeWallOfFleshGates();
		object localization = NativeLocalizationProbe.Capture();
		object frameReplay = Environment.GetEnvironmentVariable("DINGDONG_FRAME_REPLAY") == "1"
			? DingDongAuthoredStructureLoader.ProbeCloudFraming(Mod) : null;
		string folder = Path.Combine(Main.SavePath, "DingDongDiagnostics", "HeightWorld"); Directory.CreateDirectory(folder);
		string path = Path.Combine(folder, Path.GetFileNameWithoutExtension(world) + ".json");
		File.WriteAllText(path, JsonSerializer.Serialize(new {
			world, width = Main.maxTilesX, height = Main.maxTilesY, seed = Main.ActiveWorldFileData.Seed,
			Main.worldSurface, Main.rockLayer, Main.getGoodWorld, Main.remixWorld,
			geometryVersion = NativeUnderworldSystem.GeometryVersion, layer, reflectedLayer,
			left = new { left.Width, left.Bottom }, right = new { right.Width, right.Bottom }, sulphurWidth, bands, settlement, frameReplay,
			waterColumns = WaterColumns(), wallOfFleshGates, localization,
			verdict = "PASS", checks, nativeHellPredicateExecuted = true, tileEdits = frameReplay != null,
			worldSavedByProbe = false, visualAcceptance = false, liveMultiplayerAcceptance = false
		}, new JsonSerializerOptions { WriteIndented = true }));
		Mod.Logger.Info($"DINGDONG_HEIGHT_WORLD_PROBE|verdict=PASS|checks={checks}|layer={layer}|geometryVersion={NativeUnderworldSystem.GeometryVersion}|actualLoadedTiles=True|path={path}|visualAcceptance=False");
	}

	// Observe the operands produced by the real patched native summon method.
	// Return before its first branch, so the fixture cannot create a boss.
	private static object ProbeWallOfFleshGates()
	{
		var system = ModContent.GetInstance<NativeUnderworldSystem>();
		var original = new Terraria.ModLoader.IO.TagCompound(); system.SaveWorldData(original);
		var corridor = typeof(NativeUnderworldSystem).GetField("wallOfFleshLayer", BindingFlags.NonPublic | BindingFlags.Static);
		object originalCorridor = corridor.GetValue(null);
		float observedY = float.NaN, observedGate = float.NaN; int calls = 0;
		var cases = new List<object>();
		using var observe = new ILHook(typeof(NPC).GetMethod(nameof(NPC.SpawnWOF)), il => {
			var branch = il.Body.Instructions.FirstOrDefault(i => i.OpCode == Mono.Cecil.Cil.OpCodes.Blt || i.OpCode == Mono.Cecil.Cil.OpCodes.Blt_S)
				?? throw new InvalidOperationException("Native Wall of Flesh gate branch changed.");
			var cursor = new ILCursor(il); cursor.Goto(branch, MoveType.Before);
			cursor.EmitDelegate<Action<float, float>>((y, gate) => { observedY = y; observedGate = gate; calls++; });
			cursor.Emit(Mono.Cecil.Cil.OpCodes.Ret);
		});
		try
		{
			foreach (int version in new[] { 0, 1, 2 })
			{
				system.LoadWorldData(new Terraria.ModLoader.IO.TagCompound { ["nativeUnderworldGeometryVersion"] = version });
				int expected = version == 0 ? Main.maxTilesY - 205 : NativeUnderworldSystem.Layer;
				foreach (int delta in new[] { -1, 1 })
				{
					int before = calls; NPC.SpawnWOF(new Vector2(Main.maxTilesX * 8f, (expected + delta) * 16f));
					if (calls != before + 1 || observedGate != expected || observedY != expected + delta)
						throw new InvalidOperationException("Native Wall of Flesh gate operands mismatch: " + version);
				}
				cases.Add(new { version, expectedGate = expected, observedGate });
			}
		}
		finally { system.LoadWorldData(original); corridor.SetValue(null, originalCorridor); }
		return new { verdict = "PASS", calls, cases, nativeGateOperandsExecuted = true, bossSpawned = false, stateRestored = true };
	}

	private void ScheduleExit(string world)
	{
		exitRequested = 0;
		if (Environment.GetEnvironmentVariable("DINGDONG_FIXTURE_AUTOSTOP") == "1")
			exitTimer = new System.Threading.Timer(_ => {
				if (!Main.dedServ || Main.ActiveWorldFileData?.Path != world) return;
				// InitializeServer resets Disconnect. Wait until its network thread is
				// alive so the requested native exit cannot be erased by startup.
				if ((ServerThread?.GetValue(null) as System.Threading.Thread)?.IsAlive != true ||
					System.Threading.Interlocked.CompareExchange(ref exitRequested, 1, 0) != 0) return;
				Netplay.SaveOnServerExit = true; Netplay.Disconnect = true;
				Mod.Logger.Info("DINGDONG_HEIGHT_FIXTURE_EXIT|nativeServerExit=True|fixtureOnly=True");
			}, null, 10000, 1000);
	}
	private static object WaterColumns()
	{
		var columns = new List<object>();
		foreach (bool right in new[] { false, true })
		foreach (int edge in new[] { 32,64,128,256,384,512,768,1024,1536,2048,3072,4096,6144 })
		{
			int x = right ? Main.maxTilesX - 1 - edge : edge, start = -1;
			var runs = new List<object>();
			for (int y = 32; y < NativeUnderworldSystem.Layer - 32; y++)
			{
				Tile tile = Main.tile[x,y]; bool water = tile.LiquidType == LiquidID.Water && tile.LiquidAmount >= 128 &&
					(!tile.HasTile || !Main.tileSolid[tile.TileType] || tile.IsActuated);
				if (water) { if (start < 0) start = y; }
				else if (start >= 0) { if(y-start >= 8) runs.Add(new { top = start, bottom = y }); start = -1; }
			}
			columns.Add(new { right, edge, runs });
		}
		return columns;
	}

	// Explicit opt-in only, and only the isolated fixture admitted above. Exercise
	// the real foreign settlement loop; no synthetic zero-length queue can pass.
	private static object ProbeSettlement()
	{
		if (Environment.GetEnvironmentVariable("DINGDONG_SETTLEMENT_PROBE") != "1") return null;
		ModLoader.TryGetMod("CalamityMod", out Mod calamity);
		MethodInfo settle = calamity.Code.GetType("CalamityMod.CalamityUtils", true)
			.GetMethod("SettleWater", new[] { typeof(bool) });
		int updates = 0, before = Liquid.numLiquid, bufferBefore = LiquidBuffer.numLiquidBuffer;
		bool gen = WorldGen.gen, quick = Liquid.quickSettle;
		using var counter = new ILHook(typeof(Liquid).GetMethod(nameof(Liquid.UpdateLiquid)), il => {
			var c = new ILCursor(il); c.EmitDelegate<Action>(() => updates++);
		});
		try { WorldGen.gen = true; settle.Invoke(null, new object[] { false }); }
		finally { WorldGen.gen = gen; }
		if (updates == 0 || Liquid.quickSettle != quick)
			throw new InvalidOperationException("Native settlement probe did not update water or leaked quickSettle.");
		return new { verdict = "PASS", updates, before, bufferBefore, active = Liquid.numLiquid, buffer = LiquidBuffer.numLiquidBuffer,
			quickSettleRestored = true, worldGenRestored = WorldGen.gen == gen };
	}
}
