namespace DingDongBiomeHomes.Worldgen.Compiler;

internal static class WorldCompilerResidenceCavernContract
{
	internal const int Width = 760;
	internal const int Height = 380;
	internal const int FloorDepth = 56;
	internal const int InnerMargin = 12;

	internal static bool MeetsOrganicEnvelope(int envelopeWidth,
		int envelopeHeight, int left)
	{
		int top = Height - InnerMargin - envelopeHeight;
		int samples = 0, solid = 0, open = 0, naturalWall = 0;
		int radiusX = Width / 2;
		int radiusY = Height / 2;
		long limit = (long)radiusX * radiusX * radiusY * radiusY;
		for (int x = left; x < left + envelopeWidth; x += 8)
		for (int y = top; y < top + envelopeHeight; y += 8)
		{
			samples++;
			long dx = x - radiusX;
			long dy = y - radiusY;
			bool inside = dx * dx * radiusY * radiusY +
				dy * dy * radiusX * radiusX <= limit;
			if (!inside || y >= Height - FloorDepth) solid++;
			else open++;
			naturalWall++;
		}
		return samples > 0 && solid >= samples / 10 &&
			open >= samples / 20 && naturalWall >= samples / 12;
	}
}
