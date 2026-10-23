using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using MonoMod.RuntimeDetour;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace DingDongBiomeHomes.Worldgen;

// Runs in the existing dedicated load probe; no tile/player/world save writes.
internal static class NativeUnderworldProbe
{
	internal static void Run(Mod mod)
	{
		var system = ModContent.GetInstance<NativeUnderworldSystem>();
		TagCompound original = new();
		system.SaveWorldData(original);
		int checks = 0;
		void Require(bool ok, string id) { if (!ok) throw new InvalidOperationException(id); checks++; }
		// UpdateBiomes is a client routine: its latter half unconditionally uses
		// graphics filters even on dedicated. Execute its real native biome prefix
		// only, with a scoped return AFTER both native layer assignments. This is
		// not a client-rendering or socket multiplayer test.
		using var prefixOnly = new ILHook(typeof(Player).GetMethod(nameof(Player.UpdateBiomes)), il => {
			var cursor = new ILCursor(il);
			if (!cursor.TryGotoNext(MoveType.After, i => i.MatchCall("Terraria.Player", "set_ZoneRockLayerHeight")))
				throw new InvalidOperationException("Player native biome prefix changed.");
			cursor.Emit(OpCodes.Ret);
		});
		try
		{
			foreach (int version in new[] { 0, 1, 2 })
			{
				TagCompound tag = new() { ["nativeUnderworldGeometryVersion"] = version };
				system.LoadWorldData(tag);
				int expected = Main.maxTilesY - NativeUnderworldPolicy.SavedOffset(200, Main.maxTilesY, version);
				Require(NativeUnderworldSystem.Layer == expected, "UW-LIVE-SAVE-" + version);
				TagCompound saved = new(); system.SaveWorldData(saved);
				Require(saved.GetInt("nativeUnderworldGeometryVersion") == version, "UW-LIVE-TAG-" + version);
				using MemoryStream bytes = new();
				using (var writer = new BinaryWriter(bytes, System.Text.Encoding.UTF8, true)) system.NetSend(writer);
				system.ClearWorld();
				bytes.Position = 0;
				using (var reader = new BinaryReader(bytes, System.Text.Encoding.UTF8, true)) system.NetReceive(reader);
				Require(bytes.Position == bytes.Length && NativeUnderworldSystem.GeometryVersion == version && NativeUnderworldSystem.Layer == expected, "UW-LIVE-WIRE-" + version);
				Require((int)typeof(Main).GetProperty(nameof(Main.UnderworldLayer)).GetValue(null) == expected, "UW-LIVE-GETTER-" + version);
				// A load-time dedicated probe has no local renderer. A non-local
				// valid slot prevents UpdateBiomes from requesting client sky filters.
				Player sample = new() { whoAmI = (Main.myPlayer + 1) % Main.maxPlayers };
				sample.position = new Vector2(Main.maxTilesX * 8, (expected - 4) * 16);
				sample.UpdateBiomes();
				Require(!sample.ZoneUnderworldHeight, "UW-LIVE-ABOVE-" + version);
				sample.position.Y = (expected + 4) * 16;
				sample.UpdateBiomes();
				Require(sample.ZoneUnderworldHeight, "UW-LIVE-BELOW-" + version);
				mod.Logger.Info($"DINGDONG_UNDERWORLD_CONTRACT_CASE|version={version}|layer={expected}|saveAndWire=True|nativePlayerBiomePrefix=True|clientVisualsExecuted=False");
			}
			try { system.LoadWorldData(new TagCompound { ["nativeUnderworldGeometryVersion"] = 3 }); throw new Exception("UW-LIVE-UNKNOWN-ACCEPTED"); }
			catch (InvalidDataException) { checks++; }
			Require(NativeUnderworldSystem.GeometryVersion == 2, "UW-LIVE-UNKNOWN-ATOMIC");
			using var empty = new BinaryReader(new MemoryStream());
			try { system.NetReceive(empty); throw new Exception("UW-LIVE-TRUNCATED-ACCEPTED"); }
			catch (EndOfStreamException) { checks++; }
			Require(NativeUnderworldSystem.GeometryVersion == 2, "UW-LIVE-TRUNCATED-ATOMIC");
		}
		finally { system.LoadWorldData(original); }
		mod.Logger.Info($"DINGDONG_UNDERWORLD_CONTRACTS|verdict=PASS|checks={checks}|versions=0,1,2|stateRestored=True|worldSaved=False|socketMultiplayer=False|terrainGeometryProven=False");
	}
}
