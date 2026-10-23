using System;
using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen;

public sealed class ContinentalCapacityProbeSystem : ModSystem
{
	private const string ProbeEnvironmentVariable = "DINGDONG_CONTINENTAL_CAPACITY_PROBE";

	public override void PostSetupContent()
	{
		if (!string.Equals(Environment.GetEnvironmentVariable(ProbeEnvironmentVariable), "1", StringComparison.Ordinal))
			return;

		int previousWidth = Main.maxTilesX;
		int previousHeight = Main.maxTilesY;
		try
		{
			Main.maxTilesX = ContinentalWorldDimensions.Width;
			Main.maxTilesY = ContinentalWorldDimensions.Height;
			WorldGen.setWorldSize();
			ContinentalWorldCapacity.Ensure("isolated-capacity-probe");
			ValidateContinentalCapacity();
		}
		finally
		{
			Main.maxTilesX = previousWidth;
			Main.maxTilesY = previousHeight;
			WorldGen.setWorldSize();
			ContinentalWorldCapacity.Ensure("isolated-capacity-probe-restore");
		}

		(int restoredWidth, int restoredHeight) =
			ContinentalWorldCapacity.StorageForWorld(previousWidth, previousHeight);
		if (Main.tile.Width != restoredWidth || Main.tile.Height != restoredHeight)
			throw new InvalidOperationException("Continental probe did not restore vanilla tile capacity.");
		ValidateVanillaTargets();
		Mod.Logger.Info(
			$"DINGDONG_CONTINENTAL_CAPACITY_PROBE|verdict=PASS|world={ContinentalWorldDimensions.Width}x{ContinentalWorldDimensions.Height}|storage={ContinentalWorldDimensions.StorageWidth}x{ContinentalWorldDimensions.StorageHeight}|sections=140x48|wire=Int16|restored={restoredWidth}x{restoredHeight}|vanillaTargets=Small,Medium,Large");
	}

	private static void ValidateVanillaTargets()
	{
		ValidateTarget(4200, 1200, 4201, 1201);
		ValidateTarget(6400, 1800, 6401, 1801);
		ValidateTarget(8400, 2400, 8401, 2401);
	}

	private static void ValidateTarget(int worldWidth, int worldHeight, int expectedWidth, int expectedHeight)
	{
		(int width, int height) = ContinentalWorldCapacity.StorageForWorld(worldWidth, worldHeight);
		if (width != expectedWidth || height != expectedHeight)
			throw new InvalidOperationException($"Vanilla capacity target mismatch: {worldWidth}x{worldHeight}->{width}x{height}.");
	}

	private static void ValidateContinentalCapacity()
	{
		if (Main.tile.Width != ContinentalWorldDimensions.StorageWidth ||
			Main.tile.Height != ContinentalWorldDimensions.StorageHeight)
			throw new InvalidOperationException("Continental tile capacity is incomplete.");
		if (Main.maxSectionsX != 140 || Main.maxSectionsY != 48)
			throw new InvalidOperationException("Continental network section grid is invalid.");
		ValidateRemoteSections();
		ValidateSignedWireRoundtrip();
	}

	private static void ValidateRemoteSections()
	{
		if (Netplay.Clients == null)
			return;
		foreach (RemoteClient client in Netplay.Clients)
			if (client != null && (client.TileSections.GetLength(0) < 141 || client.TileSections.GetLength(1) < 49))
				throw new InvalidOperationException("Remote client section capacity is incomplete.");
	}

	private static void ValidateSignedWireRoundtrip()
	{
		using MemoryStream stream = new();
		using (BinaryWriter writer = new(stream, System.Text.Encoding.UTF8, leaveOpen: true))
		{
			writer.Write((short)ContinentalWorldDimensions.Width);
			writer.Write((short)ContinentalWorldDimensions.Height);
		}
		stream.Position = 0;
		using BinaryReader reader = new(stream);
		if (reader.ReadInt16() != ContinentalWorldDimensions.Width ||
			reader.ReadInt16() != ContinentalWorldDimensions.Height)
			throw new InvalidOperationException("Continental dimensions do not roundtrip over signed Int16.");
	}
}
