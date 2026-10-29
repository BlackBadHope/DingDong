using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

// New imports only: preserve this source object's geometry and interaction, but
// do not force its source-world Shimmer visual effect on the destination world.
// Never call from world-load/update: a player's subsequent switch state is theirs.
internal static class AuthoredSourceAmbientPolicy
{
	internal const string PresetId = "Huadiao_ElementalSkyHouse";
	internal const string ResourcePath = "Structures/PrivateHome/HuadiaoUpdated/Huadiao_ElementalSkyHouse.shstruct";
	internal const string SourceSha256 = "D9099D4B8B3E17006F1E20661E64F44361157DC7B87A8B254D3B0D8EEFDE3BF1";
	internal const int LocalX = 69, LocalY = 87, Width = 2, Height = 3;
	private const int StateFrameHeight = 54;

	// Call after native framing, while the new authored import transaction is open.
	// All six source frames are validated before the first mutation. The asset is
	// immutable; only these six live TileFrameY values change from state 1 to 0.
	internal static bool ApplyDefaultOff(Mod mod, AuthoredPreset preset, Point origin)
	{
		if (!IsTarget(preset)) return false;
		using (Stream source = mod.GetFileStream(ResourcePath))
			if (!string.Equals(Convert.ToHexString(SHA256.HashData(source)), SourceSha256, StringComparison.Ordinal))
				throw new InvalidDataException("Huadiao ambient source changed without a reviewed monolith contract.");
		RequireFrames(origin, state: 1);
		CellSnapshot[] before = new CellSnapshot[Width * Height];
		for (int x = 0; x < Width; x++)
		for (int y = 0; y < Height; y++)
			before[x * Height + y] = CellSnapshot.Read(At(origin, x, y));
		try
		{
			for (int x = 0; x < Width; x++)
			for (int y = 0; y < Height; y++)
			{
				Tile tile = At(origin, x, y);
				tile.TileFrameY = (short)(before[x * Height + y].State.TileFrameY - StateFrameHeight);
			}
			RequireFrames(origin, state: 0);
			for (int x = 0; x < Width; x++)
			for (int y = 0; y < Height; y++)
				before[x * Height + y].RequireOnlyFrameYChanged(At(origin, x, y));
		}
		catch
		{
			for (int x = 0; x < Width; x++)
			for (int y = 0; y < Height; y++)
			{
				Tile tile = At(origin, x, y);
				tile.Get<TileWallWireStateData>() = before[x * Height + y].State;
			}
			throw;
		}
		mod.Logger.Info($"DINGDONG_SOURCE_AMBIENT_POLICY|preset={PresetId}|cells=6|sourceObjectState=1|objectState=0|onlyTileFrameYChanged=True|assetUnchanged=True|playerCanSwitch=True|clientVisualVerified=False");
		return true;
	}

	// Targeted native gate callable by the isolated probe after Generate. This is
	// an object-state proof, not a client shader proof or a global SceneMetrics scan.
	internal static void ProbeDefaultOff(Mod mod, AuthoredPreset preset, Point origin)
	{
		if (!IsTarget(preset)) return;
		RequireFrames(origin, state: 0);
		mod.Logger.Info($"DINGDONG_SOURCE_AMBIENT_PROBE|verdict=PASS|preset={PresetId}|nativeFrameCells=6|objectState=0|sceneMetricsScanned=False|clientVisualVerified=False");
	}

	private static bool IsTarget(AuthoredPreset preset)
	{
		if (preset.Id != PresetId) return false;
		if (preset.ResourcePath != ResourcePath || preset.Width != 190 || preset.Height != 160)
			throw new InvalidDataException("Huadiao ambient policy requires the unchanged 190x160 reviewed source.");
		return true;
	}

	private static Tile At(Point origin, int x, int y) => Main.tile[origin.X + LocalX + x, origin.Y + LocalY + y];

	private static void RequireFrames(Point origin, int state)
	{
		if (origin.X + LocalX < 0 || origin.Y + LocalY < 0 ||
			origin.X + LocalX + Width > Main.maxTilesX || origin.Y + LocalY + Height > Main.maxTilesY)
			throw new ArgumentOutOfRangeException(nameof(origin), "Huadiao monolith is outside the world.");
		for (int x = 0; x < Width; x++)
		for (int y = 0; y < Height; y++)
		{
			Tile tile = At(origin, x, y);
			if (!tile.HasTile || tile.TileType != TileID.ShimmerMonolith ||
				tile.TileFrameX != x * 18 || tile.TileFrameY != state * StateFrameHeight + y * 18)
				throw new InvalidOperationException($"Huadiao monolith frame mismatch at {LocalX + x},{LocalY + y}; expected state {state}.");
		}
	}

	private readonly record struct CellSnapshot(TileTypeData Type, WallTypeData Wall, LiquidData Liquid,
		TileWallBrightnessInvisibilityData Brightness, TileWallWireStateData State)
	{
		internal static CellSnapshot Read(Tile tile) => new(tile.Get<TileTypeData>(), tile.Get<WallTypeData>(),
			tile.Get<LiquidData>(), tile.Get<TileWallBrightnessInvisibilityData>(), tile.Get<TileWallWireStateData>());

		internal void RequireOnlyFrameYChanged(Tile tile)
		{
			TileWallWireStateData expectedState = State;
			expectedState.TileFrameY -= StateFrameHeight;
			if (!EqualityComparer<TileTypeData>.Default.Equals(Type, tile.Get<TileTypeData>()) ||
				!EqualityComparer<WallTypeData>.Default.Equals(Wall, tile.Get<WallTypeData>()) ||
				!EqualityComparer<LiquidData>.Default.Equals(Liquid, tile.Get<LiquidData>()) ||
				!EqualityComparer<TileWallBrightnessInvisibilityData>.Default.Equals(Brightness, tile.Get<TileWallBrightnessInvisibilityData>()) ||
				!EqualityComparer<TileWallWireStateData>.Default.Equals(expectedState, tile.Get<TileWallWireStateData>()))
				throw new InvalidOperationException("Huadiao ambient policy changed a tile plane beyond the six TileFrameY values.");
		}
	}
}
