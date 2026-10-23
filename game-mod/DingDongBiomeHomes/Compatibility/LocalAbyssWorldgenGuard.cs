// Original local compatibility guards, integrated into the public DingDong project.
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Compatibility.LegacyPack;

public sealed class AbyssWorldgenGuard : ModSystem
{
	private static volatile bool _insideWorldGeneration;

	[ThreadStatic]
	private static bool _insideCalamityAbyss;

	[ThreadStatic]
	private static HashSet<long> _activeTileFrames;

	[ThreadStatic]
	private static int _preventedRecursiveFrames;

	public override void Load()
	{
		if (!ModLoader.TryGetMod("CalamityMod", out Mod calamity))
			return;

		Type abyssType = calamity.Code.GetType("CalamityMod.World.Abyss");
		MethodInfo placeAbyss = abyssType?.GetMethod(
			"PlaceAbyss",
			BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
			null,
			Type.EmptyTypes,
			null);

		MethodInfo killTile = typeof(WorldGen).GetMethod(
			nameof(WorldGen.KillTile),
			BindingFlags.Public | BindingFlags.Static,
			null,
			new[] { typeof(int), typeof(int), typeof(bool), typeof(bool), typeof(bool) },
			null);

		MethodInfo tileFrame = typeof(WorldGen).GetMethod(
			nameof(WorldGen.TileFrame),
			BindingFlags.Public | BindingFlags.Static,
			null,
			new[] { typeof(int), typeof(int), typeof(bool), typeof(bool) },
			null);

		MethodInfo killTileDropItems = typeof(WorldGen).GetMethod(
			"KillTile_DropItems",
			BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
			null,
			new[] { typeof(int), typeof(int), typeof(Tile), typeof(bool), typeof(bool) },
			null);

		if (placeAbyss == null || killTile == null || tileFrame == null || killTileDropItems == null)
		{
			Mod.Logger.Warn("Calamity Abyss recursion guard was not installed because an expected method signature changed.");
			return;
		}

		MonoModHooks.Add(placeAbyss, RunAbyssWithGuard);
		MonoModHooks.Add(killTile, KillTileWithoutDropsDuringWorldgen);
		MonoModHooks.Add(tileFrame, PreventRecursiveTileFrameDuringAbyss);
		MonoModHooks.Add(killTileDropItems, SuppressItemDropsDuringWorldgen);
		Mod.Logger.Info("Installed Calamity Abyss / mod-tile recursion guard.");
	}

	public override void PreWorldGen()
	{
		_insideWorldGeneration = true;
	}

	public override void PostWorldGen()
	{
		RemoveInvalidCalamityHologramEntities();
		_insideWorldGeneration = false;
	}

	private void RemoveInvalidCalamityHologramEntities()
	{
		List<TileEntity> invalid = new();
		foreach (TileEntity entity in TileEntity.ByPosition.Values)
		{
			if (entity.GetType().FullName != "CalamityMod.TileEntities.TELabHologramProjector" || entity is not ModTileEntity modEntity)
				continue;

			bool valid;
			try
			{
				valid = modEntity.IsTileValidForEntity(entity.Position.X, entity.Position.Y);
			}
			catch (Exception exception)
			{
				valid = false;
				Mod.Logger.Warn($"Calamity hologram TileEntity validation failed at {entity.Position.X},{entity.Position.Y}: {exception.GetType().Name}: {exception.Message}");
			}

			if (!valid)
				invalid.Add(entity);
		}

		foreach (TileEntity entity in invalid)
		{
			TileEntity.ByPosition.Remove(entity.Position);
			TileEntity.ByID.Remove(entity.ID);
		}

		if (invalid.Count > 0)
			Mod.Logger.Warn($"Removed {invalid.Count} dangling Calamity Lab Hologram Projector TileEntity record(s) before world save; world tiles were not changed.");
	}

	public override void OnWorldLoad()
	{
		_insideWorldGeneration = false;
	}

	public override void OnWorldUnload()
	{
		_insideWorldGeneration = false;
	}

	private static void RunAbyssWithGuard(PlaceAbyss orig)
	{
		bool previous = _insideCalamityAbyss;
		_insideCalamityAbyss = true;
		_preventedRecursiveFrames = 0;
		_activeTileFrames ??= new HashSet<long>();
		_activeTileFrames.Clear();

		try
		{
			orig();
		}
		finally
		{
			int prevented = _preventedRecursiveFrames;
			_activeTileFrames.Clear();
			_insideCalamityAbyss = previous;
			if (prevented > 0)
				ModContent.GetInstance<AbyssWorldgenGuard>().Mod.Logger.Warn($"Prevented {prevented} recursive TileFrame call(s) while Calamity generated the Abyss.");
		}
	}

	private static void KillTileWithoutDropsDuringWorldgen(
		KillTile orig,
		int i,
		int j,
		bool fail,
		bool effectOnly,
		bool noItem)
	{
		// Item drops have no useful consumer while the world is being generated.
		// Forcing noItem also keeps player-dependent GlobalItem hooks from running
		// on a dedicated server before Main.LocalPlayer has a complete ModPlayer array.
		orig(i, j, fail, effectOnly, noItem || WorldGen.gen || _insideWorldGeneration);
	}

	private static void PreventRecursiveTileFrameDuringAbyss(
		TileFrame orig,
		int i,
		int j,
		bool resetFrame,
		bool noBreak)
	{
		// Mod tiles can recursively ask Terraria to frame the same coordinate
		// outside the Abyss too (notably while vanilla minecart tracks generate).
		// Guard only an actual re-entry into a coordinate already on this thread;
		// independent and neighbouring frame operations still run normally.
		_activeTileFrames ??= new HashSet<long>();
		long key = ((long)i << 32) | (uint)j;
		if (!_activeTileFrames.Add(key))
		{
			if (_insideCalamityAbyss)
				_preventedRecursiveFrames++;
			return;
		}

		try
		{
			orig(i, j, resetFrame, noBreak);
		}
		finally
		{
			_activeTileFrames.Remove(key);
		}
	}

	private static void SuppressItemDropsDuringWorldgen(
		KillTileDropItems orig,
		int x,
		int y,
		Tile tileCache,
		bool includeLargeObjectDrops,
		bool includeAllModdedLargeObjectDrops)
	{
		if (WorldGen.gen || _insideWorldGeneration)
			return;

		orig(x, y, tileCache, includeLargeObjectDrops, includeAllModdedLargeObjectDrops);
	}

	private delegate void PlaceAbyss();
	private delegate void KillTile(int i, int j, bool fail, bool effectOnly, bool noItem);
	private delegate void TileFrame(int i, int j, bool resetFrame, bool noBreak);
	private delegate void KillTileDropItems(int x, int y, Tile tileCache, bool includeLargeObjectDrops, bool includeAllModdedLargeObjectDrops);
}
