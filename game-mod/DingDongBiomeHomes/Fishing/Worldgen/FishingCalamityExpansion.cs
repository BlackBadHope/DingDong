using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DingDongBiomeHomes.Worldgen;
using DingDongBiomeHomes.Worldgen.Exact66Adapters;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Fishing.Worldgen;

public sealed class FishingCalamityExpansion : ModSystem
{
	private static readonly List<IDisposable> hooks = new();
	private static bool supported, present;
	private static string unavailable;
	private static Type abyssType;
	private static Mod provider;
	internal static bool Enabled => FishingWaterWorldSystem.Generating || FishingWaterWorldSystem.ExpandedWorld;
	private static PropertyInfo seaStart;
	private static MethodInfo determineSeaStart;
	private static PropertyInfo seaDepth;
	private static Func<int> seaWidth;

	public override void PostSetupContent()
	{
		present = ModLoader.TryGetMod("CalamityMod", out provider);
		if (!present) { supported = true; return; }
		try
		{
			provider = Exact66ProviderGate.Require("CalamityMod", new Version(2, 2, 2), "0e63c698-ec72-446e-ac1f-8d01f0ee7ac7");
			abyssType = provider.Code.GetType("CalamityMod.World.Abyss", true);
			Type sea = provider.Code.GetType("CalamityMod.World.SulphurousSea", true);
			seaStart = sea.GetProperty("YStart");
			determineSeaStart = sea.GetMethod("DetermineYStart");
			seaDepth = sea.GetProperty("BlockDepth");
			seaWidth = sea.GetProperty("BiomeWidth").GetMethod.CreateDelegate<Func<int>>();
			hooks.Add(new ILHook(sea.GetMethod("PlaceSulphurSea"), PatchSeaStart));
			hooks.Add(FishingSulphurTreasureGuard.Install(sea));
			hooks.Add(new ILHook(sea.GetProperty("BiomeWidth").GetMethod, PatchSeaWidth));
			hooks.Add(new ILHook(sea.GetProperty("BlockDepth").GetMethod, PatchSeaDepth));
			hooks.Add(new ILHook(abyssType.GetMethod("PlaceAbyss"), PatchAbyss));
			Type biome = provider.Code.GetType("CalamityMod.BiomeManagers.AbyssLayer1Biome", true);
			var gate = biome.GetMethod("MeetsBaseAbyssRequirement", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			hooks.Add(new ILHook(gate, PatchAbyssGate));
			Type sulphurBiome = provider.Code.GetType("CalamityMod.BiomeManagers.SulphurousSeaBiome", true);
			hooks.Add(new ILHook(sulphurBiome.GetMethod("IsInBiomePosition", new[] { typeof(Microsoft.Xna.Framework.Point) }), PatchSulphurGate));
			supported = true;
		}
		catch (Exception error)
		{
			foreach (var hook in hooks.AsEnumerable().Reverse()) hook.Dispose();
			hooks.Clear(); supported = false;
			unavailable = error.GetType().Name + ": " + error.Message;
			Mod.Logger.Warn("Fishing expanded Calamity generation unavailable: " + unavailable + ". Existing-world play remains available.");
		}
	}
	internal static void RequireSupported()
	{
		if (present && !supported) throw new InvalidOperationException("Fishing worldgen requires the verified Calamity 2.2.2 adapter: " + unavailable);
	}
	internal static bool IsAbyssPosition(int x, int y)
	{
		if (!present || abyssType == null || seaStart == null || Main.remixWorld) return false;
		bool left = (bool)abyssType.GetField("AtLeftSideOfWorld").GetValue(null);
		int seaY = (int)seaStart.GetValue(null);
		int edge = left ? x : Main.maxTilesX - x;
		return edge < (Enabled ? 930 : 310) && y >= (seaY + (int)Main.worldSurface) / 2 + 90 && y <= NativeUnderworldSystem.Layer;
	}
	internal static bool TryLegacyCoast(bool right, out NativeCoastBand band)
	{
		band = default;
		if (!supported || !present || Enabled || Main.remixWorld) return false;
		bool left = (bool)abyssType.GetField("AtLeftSideOfWorld").GetValue(null);
		if (right == left) return false;
		int width = seaWidth(), bottom = (int)seaStart.GetValue(null) + (int)seaDepth.GetValue(null);
		band = new NativeCoastBand(width, Math.Min(bottom, NativeUnderworldSystem.Layer - 1));
		return band.Valid(Main.maxTilesX, Main.maxTilesY);
	}
	public override void Unload()
	{
		foreach (var hook in hooks.AsEnumerable().Reverse()) hook.Dispose();
		hooks.Clear(); present = supported = false; abyssType = null; provider = null;
		FishingSulphurTreasureGuard.Reset();
		seaStart = seaDepth = null; determineSeaStart = null; seaWidth = null;
	}
	private static void ReturnMap(ILContext il, Func<int, int> map)
	{
		var returns = il.Body.Instructions.Where(i => i.OpCode == OpCodes.Ret).ToArray();
		if (returns.Length == 0) throw new InvalidOperationException("Calamity getter has no return.");
		foreach (var instruction in returns) { var c = new ILCursor(il); c.Goto(instruction, MoveType.AfterLabel); c.EmitDelegate(map); }
	}
	private static void PatchSeaWidth(ILContext il) => ReturnMap(il, native => Enabled && FishingWaterWorldSystem.SulphurWidth > 0 ? FishingWaterWorldSystem.SulphurWidth : native);
	private static void PatchSeaDepth(ILContext il) => ReturnMap(il, native => Enabled ? FishingWaterGeometry.OceanDepth(native, Main.maxTilesY) : native);
	private static void PatchSeaStart(ILContext il)
	{
		var c = new ILCursor(il); c.EmitDelegate<Action>(PrepareSeaBounds);
	}
	private static void PrepareSeaBounds()
	{
		if (!FishingWaterWorldSystem.Generating) return;
		int native = (int)(Main.maxTilesX / 16.8f);
		int desired = FishingWaterGeometry.OceanWidth(Main.maxTilesX, native);
		bool left = Main.dungeonX < Main.maxTilesX / 2;
		int dungeonDistance = left ? Main.dungeonX : Main.maxTilesX - Main.dungeonX;
		if (GenVars.dMinX > 0 && GenVars.dMaxX > GenVars.dMinX && GenVars.dMaxX < Main.maxTilesX)
			dungeonDistance = Math.Min(dungeonDistance, left ? GenVars.dMinX : Main.maxTilesX - GenVars.dMaxX);
		int safe = Math.Min(desired, dungeonDistance - NativeCoastalPolicy.BeachAndGraveyardMargin - 200);
		// Calamity chooses its YStart from the future inland shore. Its sea
		// never edits a whole world-height column: sky houses and deep labs
		// outside this envelope must not veto an otherwise valid coastline.
		bool oldSide = (bool)abyssType.GetField("AtLeftSideOfWorld").GetValue(null);
		abyssType.GetField("AtLeftSideOfWorld").SetValue(null, left);
		bool[] mask = ContinentalPlacementMaskPolicy.CreateStructureOnly();
		int conflict = -1, top = 0, bottom = 0;
		try
		{
			while (safe >= 1000)
			{
				FishingWaterWorldSystem.SulphurWidth = safe;
				determineSeaStart.Invoke(null, null);
				int startY = (int)seaStart.GetValue(null);
				int depth = (int)seaDepth.GetValue(null);
				top = Math.Max(40, startY - NativeCoastalPolicy.Clearance(150, Main.maxTilesY, true));
				bottom = Math.Min(NativeUnderworldSystem.Layer - 40,
					Math.Max((int)Main.rockLayer + 64, startY + depth + 64));
				NativeReservationProbe.Sea(ModContent.GetInstance<FishingCalamityExpansion>().Mod, left, safe, top, bottom);
				int previous = safe;
				for (int edge = 1000; edge < safe; edge += 32)
				{
					var strip = new Microsoft.Xna.Framework.Rectangle(left ? edge : Main.maxTilesX - edge - 32,
						top, 32, bottom - top);
					if (GenVars.structures.CanPlace(strip, mask, 40)) continue;
					conflict = edge; safe = edge - 40; break;
				}
				if (safe == previous) break;
			}
		}
		finally { abyssType.GetField("AtLeftSideOfWorld").SetValue(null, oldSide); }
		ModContent.GetInstance<FishingCalamityExpansion>().Mod.Logger.Info(
			$"DINGDONG_SEA_BOUNDS|desired={desired}|safe={safe}|dungeonDistance={dungeonDistance}|top={top}|bottom={bottom}|conflict={conflict}");
		if (safe < 1000) throw new InvalidOperationException(
			$"Expanded Abyss/sea overlaps a protected structure inside its actual envelope (edge={conflict}, top={top}, bottom={bottom}); generation refused.");
		FishingWaterWorldSystem.SulphurWidth = safe;
	}
	private static void PatchAbyss(ILContext il)
	{
		// The exact provider MVID and the local/constant topology are both required.
		var center = il.Body.Instructions.Where(i => i.MatchStloc(5)).ToArray();
		var radii = il.Body.Instructions.Where(i => i.MatchLdcI4(160) && i.Offset < 0x100).ToArray();
		if (center.Length != 1 || radii.Length != 2 || il.Body.Variables.Count < 40)
			throw new InvalidOperationException("Calamity Abyss horizontal topology changed.");
		var c = new ILCursor(il); c.Goto(center[0], MoveType.Before);
		c.EmitDelegate<Func<int, int>>(native => !FishingWaterWorldSystem.Generating ? native :
			native < Main.maxTilesX / 2 ? native * 3 : Main.maxTilesX - (Main.maxTilesX - native) * 3);
		foreach (var radius in radii)
		{
			c.Goto(radius, MoveType.After);
			c.EmitDelegate<Func<int, int>>(native => FishingWaterWorldSystem.Generating ? native * 3 : native);
		}
		var returns = il.Body.Instructions.Where(i => i.OpCode == OpCodes.Ret).ToArray();
		foreach (var ret in returns) { c.Goto(ret, MoveType.AfterLabel); c.EmitDelegate<Action>(FinishAbyss); }
	}
	private static void PatchAbyssGate(ILContext il)
	{
		var radii = il.Body.Instructions.Where(i => i.MatchLdcI4(140)).ToArray();
		if (radii.Length != 2 || radii[0].Next.OpCode != OpCodes.Add || radii[1].Next.OpCode != OpCodes.Sub)
			throw new InvalidOperationException("Calamity Abyss biome X gate changed.");
		foreach (var site in radii)
		{
			var c = new ILCursor(il); c.Goto(site, MoveType.After);
			c.EmitDelegate<Func<int, int>>(radius => Enabled ? 930 - 170 : radius);
		}
	}
	private static void PatchSulphurGate(ILContext il)
	{
		var widths = il.Body.Instructions.Where(i => i.MatchLdcI4(435)).ToArray();
		if (widths.Length != 2) throw new InvalidOperationException("Calamity sulphur biome horizontal gate changed.");
		foreach (var width in widths)
		{
			var c = new ILCursor(il); c.Goto(width, MoveType.After);
			c.EmitDelegate<Func<int, int>>(native => {
				if (Enabled && FishingWaterWorldSystem.SulphurWidth > 0) return FishingWaterWorldSystem.SulphurWidth;
				return ContinentalWorldDimensions.IsContinental && !Main.remixWorld && !WorldGen.gen ? seaWidth() : native;
			});
		}
	}
	private static void FinishAbyss()
	{
		if (!FishingWaterWorldSystem.Generating) return;
		bool left = (bool)abyssType.GetField("AtLeftSideOfWorld").GetValue(null);
		int bottom = (int)abyssType.GetField("AbyssChasmBottom").GetValue(null);
		int top = Math.Max(40, (int)Main.worldSurface + 30);
		FishingWaterWorldSystem.RecordAbyss(left, top, bottom);
		HashSet<ushort> abyssTiles = new();
		foreach (string name in new[] { "SulphurousShale", "AbyssGravel", "PyreMantle", "Voidstone" })
			if (provider.TryFind<ModTile>(name, out var tile)) abyssTiles.Add((ushort)tile.Type);
		if (abyssTiles.Count != 4) throw new InvalidOperationException("Calamity Abyss layer tile IDs unavailable.");
		HashSet<long> chestCells = new();
		foreach (Chest chest in Main.chest)
		{
			if (chest == null) continue;
			for (int xx = (chest.x - 8) / 16; xx <= (chest.x + 10) / 16; xx++)
				for (int yy = (chest.y - 8) / 16; yy <= (chest.y + 10) / 16; yy++)
					chestCells.Add(((long)xx << 32) | (uint)yy);
		}
		// Native center, layers, shrine and fossils stay intact. Side shafts and
		// intermittent cross-routes make the wider footprint traversable.
		// V03 28.10: route shape moved to AbyssRouteGeometry (no more lattice, 15.webp).
		int routeBottom = bottom - AbyssRouteGeometry.BottomMargin;
		int crossings = AbyssRouteGeometry.CrossingCount(top, routeBottom);
		for (int y = top; y < routeBottom; y++)
		{
			for (int edge = AbyssRouteGeometry.EdgeFrom; edge < AbyssRouteGeometry.EdgeTo; edge++)
			{
				if (!AbyssRouteGeometry.InRoute(edge, y, top, routeBottom, crossings)) continue;
				int x = left ? edge : Main.maxTilesX - edge;
				if (chestCells.Contains(((long)(x / 16) << 32) | (uint)(y / 16))) continue;
				Tile t = Main.tile[x, y];
				if (t.HasTile && !abyssTiles.Contains(t.TileType)) continue;
				// Never remove a native chest, shrine, ore, fossil or decoration.
				t.ClearTile(); t.LiquidType = LiquidID.Water; t.LiquidAmount = 255;
			}
		}
		// Roadmap item 7: the routes above are Ding Dong's own; give every
		// crossing waystations and debris so the floors lead somewhere.
		OceanExpeditions.AbyssWaystations.Decorate(ModContent.GetInstance<FishingCalamityExpansion>().Mod, left, top, bottom);
	}
}
