using System;
using System.IO;
using System.Text.Json;
using Terraria;
using Terraria.Map;

namespace DingDongBiomeHomes.Worldgen;

// Diagnostic geometry raster, not a replacement for the in-game map renderer.
internal static class NativeTerrainRaster
{
	internal static void ExportRegion(string path, Microsoft.Xna.Framework.Rectangle area, int step = 1)
	{
		area = Microsoft.Xna.Framework.Rectangle.Intersect(area, new(0, 0, Main.maxTilesX, Main.maxTilesY));
		if (area.Width <= 0 || area.Height <= 0) throw new ArgumentOutOfRangeException(nameof(area));
		if (step < 1) throw new ArgumentOutOfRangeException(nameof(step));
		Write(path, new NativeTerrainPalette(), area.X, area.Y, area.Width, area.Height, step);
	}

	internal static void Export()
	{
		string directory = Path.Combine(Main.SavePath, "DingDongDiagnostics", "TerrainRaster",
			DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff"));
		Directory.CreateDirectory(directory);
		MapHelper.Initialize(); // Palette only; no WorldMap allocation or world mutation.
		NativeTerrainPalette palette = new();
		Write(Path.Combine(directory, "overview.bmp"), palette, 0, 0, Main.maxTilesX, Main.maxTilesY, 4);
		int cropWidth = Math.Min(1400, Main.maxTilesX), cropHeight = Math.Min(1000, Main.maxTilesY);
		for (int i = 1; i <= 4; i++)
		{
			int x = Math.Clamp(Main.maxTilesX * i / 5 - cropWidth / 2, 0, Main.maxTilesX - cropWidth);
			int y = Math.Clamp((int)Main.worldSurface - 200, 0, Main.maxTilesY - cropHeight);
			Write(Path.Combine(directory, $"crop-{i}-x{x}-y{y}.bmp"), palette, x, y, cropWidth, cropHeight, 1);
		}
		File.WriteAllText(Path.Combine(directory, "provenance.json"), JsonSerializer.Serialize(new {
			world = Main.ActiveWorldFileData.Path, seed = Main.ActiveWorldFileData.Seed,
			width = Main.maxTilesX, height = Main.maxTilesY,
			overviewSampling = "one actual tile per 4x4 block; thin features may alias; crops are 1:1",
			palette = "vanilla base map entries; deterministic diagnostic colors for mod tile IDs; no texture/frame/paint rendering",
			worldMutated = false, generatedByAI = false
		}));
		Console.WriteLine("DINGDONG_TERRAIN_RASTER|directory=" + directory);
	}

	private static void Write(string path, NativeTerrainPalette palette, int left, int top, int width, int height, int step)
	{
		int w = (width + step - 1) / step, h = (height + step - 1) / step;
		int stride = (w * 3 + 3) & ~3;
		using BinaryWriter writer = new(File.Create(path));
		writer.Write((ushort)0x4D42); writer.Write(checked(54 + stride * h));
		writer.Write(0); writer.Write(54); writer.Write(40); writer.Write(w); writer.Write(-h);
		writer.Write((ushort)1); writer.Write((ushort)24); writer.Write(0); writer.Write(stride * h);
		writer.Write(2835); writer.Write(2835); writer.Write(0); writer.Write(0);
		byte[] row = new byte[stride];
		for (int y = 0; y < h; y++)
		{
			for (int x = 0; x < w; x++)
			{
				var color = palette.At(left + x * step, top + y * step);
				row[x * 3] = color.B; row[x * 3 + 1] = color.G; row[x * 3 + 2] = color.R;
			}
			writer.Write(row);
		}
	}
}
