using System;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static partial class CalamityWorldgenAdapter
{
	private static void PatchFullDesertCleanup(ILContext il)
	{
		ILCursor cursor = new(il);
		Instruction original = cursor.Next ?? throw new InvalidOperationException(
			"Vanilla desert cleanup method is empty.");
		cursor.Emit(OpCodes.Ldarg_0);
		cursor.EmitDelegate<Func<Rectangle, bool>>(TryOptimizedDesertCleanup);
		cursor.Emit(OpCodes.Brfalse, original);
		cursor.Emit(OpCodes.Ret);
	}

	private static bool TryOptimizedDesertCleanup(Rectangle area)
	{
		if (!ContinentalAdapterMath.Active()) return false;

		int left = Math.Max(1, area.Left - 20);
		int right = Math.Min(Main.maxTilesX - 2, area.Right + 19);
		int top = Math.Max(1, area.Top - 20);
		int bottom = Math.Min(Main.maxTilesY - 2, area.Bottom + 19);
		if (left > right || top > bottom)
		{
			CalamityWorldgenReceipt.MarkDesertCleanup(0);
			return true;
		}

		long frames = 0;
		for (int x = left; x <= right; x++)
		for (int y = top; y <= bottom; y++)
		{
			WorldGen.TileFrame(x, y, true, false);
			Framing.WallFrame(x, y, true);
			frames += 2;
		}

		int borderLeft = Math.Max(1, left - 1);
		int borderRight = Math.Min(Main.maxTilesX - 2, right + 1);
		int borderTop = Math.Max(1, top - 1);
		int borderBottom = Math.Min(Main.maxTilesY - 2, bottom + 1);
		for (int x = borderLeft; x <= borderRight; x++)
		{
			FrameDesertBorder(x, borderTop, x >= left && x <= right, ref frames);
			if (borderBottom != borderTop)
				FrameDesertBorder(x, borderBottom, x >= left && x <= right, ref frames);
		}
		for (int y = top; y <= bottom; y++)
		{
			FrameDesertBorder(borderLeft, y, true, ref frames);
			if (borderRight != borderLeft)
				FrameDesertBorder(borderRight, y, true, ref frames);
		}

		CalamityWorldgenReceipt.MarkDesertCleanup(frames);
		return true;
	}

	private static void FrameDesertBorder(int x, int y, bool frameTile,
		ref long frames)
	{
		if (frameTile)
		{
			WorldGen.TileFrame(x, y, true, false);
			frames++;
		}
		Framing.WallFrame(x, y, false);
		frames++;
	}
}
