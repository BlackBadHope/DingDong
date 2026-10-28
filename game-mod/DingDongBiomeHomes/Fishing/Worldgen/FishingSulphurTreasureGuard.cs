using System;
using System.Linq;
using System.Reflection;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.ModLoader;
using Terraria.WorldBuilding;
using XnaPoint = Microsoft.Xna.Framework.Point;

namespace DingDongBiomeHomes.Fishing.Worldgen;

// Calamity 2.2.2 SulphurousSea.GenerateTreasureChest retries its island chest
// column by column (`while (!try(p)) p.X += dir`). The try itself has two
// unguarded paths: WorldUtils.Find failing yields GenSearch.NOT_FOUND
// (int.MaxValue) and the "bury until enclosed" loop can walk past the world
// edge. Both end in IndexOutOfRangeException before the first tile edit.
// The expanded Abyss chasm sits at a fixed 3*170 = 510 cells from the edge,
// while the island chest sits at 0.18 * SulphurWidth; on the 16800-wide
// preset (SulphurWidth 2800 -> 504) the chest column is the open chasm.
// This guard replays only the read-only prefix of the provider routine. A
// column is rejected exactly when the native code would have thrown, so every
// column the native code accepts reaches it unchanged (no RNG, no tile edits).
internal static class FishingSulphurTreasureGuard
{
	private delegate bool TreasureOrig(XnaPoint chestPoint);
	private delegate bool TreasureHook(TreasureOrig orig, XnaPoint chestPoint);
	private const string Prefix = "<GenerateTreasureChest>g__tryToGenerateTreasureChest|";
	private static int rejected, firstRejectedX = -1;

	internal static IDisposable Install(Type sea)
	{
		MethodInfo target = sea.GetMethods(BindingFlags.Static | BindingFlags.NonPublic).Single(m =>
			m.Name.StartsWith(Prefix, StringComparison.Ordinal) && m.ReturnType == typeof(bool) &&
			m.GetParameters().Select(p => p.ParameterType).SequenceEqual(new[] { typeof(XnaPoint) }));
		rejected = 0; firstRejectedX = -1;
		return new Hook(target, new TreasureHook(Guard));
	}

	private static bool Guard(TreasureOrig orig, XnaPoint chestPoint)
	{
		if (!FishingWaterWorldSystem.Generating) return orig(chestPoint);
		// Past these columns the native routine can only throw or retry outward forever.
		if (chestPoint.X < 2 || chestPoint.X > Main.maxTilesX - 4)
			throw new InvalidOperationException($"Calamity sulphur treasure search left the world (x={chestPoint.X}, rejected={rejected}, firstRejectedX={firstRejectedX}).");
		if (!WouldThrow(chestPoint))
		{
			bool placed = orig(chestPoint);
			if (placed && rejected > 0)
			{
				ModContent.GetInstance<FishingCalamityExpansion>().Mod.Logger.Info(
					$"DINGDONG_SULPHUR_TREASURE|rejectedColumns={rejected}|firstRejectedX={firstRejectedX}|placedX={chestPoint.X}|reason=native-out-of-range");
				rejected = 0; firstRejectedX = -1;
			}
			return placed;
		}
		if (rejected++ == 0) firstRejectedX = chestPoint.X;
		return false;
	}

	// Exact replay of the native prefix up to the first tile mutation.
	private static bool WouldThrow(XnaPoint start)
	{
		if (!WorldUtils.Find(start, Searches.Chain(new Searches.Down(300), new Conditions.IsSolid()), out XnaPoint p))
			return true;
		int x = p.X, y = p.Y;
		int digDepth = 0;
		while (true)
		{
			bool down = SolidAndValid(x, y + digDepth), downRight = SolidAndValid(x + 1, y + digDepth);
			if (digDepth >= 32 && (!down || !downRight)) break;
			digDepth++;
			if (digDepth >= 80) return false; // native returns false itself
		}
		y += digDepth - 12;
		while (true)
		{
			if (x - 2 < 0 || x + 3 >= Main.maxTilesX || y - 2 < 0 || y + 1 >= Main.maxTilesY) return true;
			bool closed = true;
			for (int dx = -2; dx < 4 && closed; dx++)
				for (int dy = -1; dy < 3; dy++)
					if (!Main.tile[x + dx, y - dy].HasTile) { closed = false; break; }
			if (closed) break;
			y++;
		}
		// Surface marker cells written after the chest is placed.
		return p.X + 1 >= Main.maxTilesX || p.Y - 1 < 0 || p.Y + 2 >= Main.maxTilesY;
	}

	// CalamityUtils.ParanoidTileRetrieval + `HasTile && IsTileSolid()`.
	private static bool SolidAndValid(int x, int y)
	{
		Tile t = WorldGen.InWorld(x, y) ? Main.tile[x, y] : new Tile();
		return t.HasTile && t.HasUnactuatedTile && Main.tileSolid[t.TileType] && !Main.tileSolidTop[t.TileType];
	}

	internal static void Reset() { rejected = 0; firstRejectedX = -1; }
}
