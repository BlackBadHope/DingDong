using System;
using System.IO;

namespace DingDongBiomeHomes.Worldgen;

internal static partial class ContinentalSaveStreamHook
{
	private enum Plane { None, Tile, Wall }

	[ThreadStatic] private static Plane activePlane;
	[ThreadStatic] private static long activeRawBytes;

	private static MemoryStream CreatePlaneStream(object implementation)
	{
		// A failed native SaveData never reaches TakeEncodedBuffer. Its state
		// must not turn the next plain stream on this thread into headerless gzip.
		activePlane = Plane.None;
		activeRawBytes = 0;
		if (!ContinentalWorldDimensions.IsContinental)
			return new MemoryStream();
		Plane plane = implementation.GetType().Name switch
		{
			"TileIOImpl" => Plane.Tile,
			"WallIOImpl" => Plane.Wall,
			_ => Plane.None
		};
		if (plane == Plane.None)
			return new MemoryStream();
		ContinentalSaveBudgetReceipt receipt = ContinentalSaveBudget.MeasureOnce();
		long rawBytes = plane == Plane.Tile ? receipt.TileDataBytes :
			receipt.WallDataBytes;
		if (rawBytes <= 0 || rawBytes > ContinentalSaveBudget.RawPlaneCapBytes)
			throw new InsufficientMemoryException(
				$"Continental {plane} save plane is invalid: {rawBytes}.");
		activePlane = plane;
		activeRawBytes = rawBytes;
		return ContinentalSavePlaneCodec.Begin(checked((int)rawBytes));
	}

	private static BinaryWriter CreatePlaneWriter(Stream output) =>
		activePlane == Plane.None
			? ContinentalSavePlaneCodec.CreatePlainWriter(output)
			: ContinentalSavePlaneCodec.CreateEncodedWriter(output);

	private static void FinishPlaneWriter(BinaryWriter writer) => writer.Dispose();

	private static byte[] TakeEncodedBuffer(MemoryStream stream)
	{
		Plane plane = activePlane;
		long rawBytes = activeRawBytes;
		activePlane = Plane.None;
		activeRawBytes = 0;
		if (plane != Plane.None)
			ContinentalSavePlaneCodec.Seal(stream);
		byte[] result = stream.ToArray();
		if (plane != Plane.None)
			DingDongBiomeHomes.Instance.Logger.Info(
				$"DINGDONG_CONTINENTAL_SAVE_STREAM|verdict=PASS|plane={plane}" +
				$"|rawBytes={rawBytes}|encodedBytes={result.LongLength}" +
				$"|savedBytes={rawBytes - result.LongLength}");
		if (plane == Plane.Wall)
			ContinentalSaveBudget.Reset();
		return result;
	}

	private static Stream OpenPlane(byte[] source) =>
		ContinentalSavePlaneCodec.OpenRead(source);
}
