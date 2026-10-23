using System;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static class WorldCompilerFinalizer
{
	internal static long Finalize(WorldCompilerPlan plan)
	{
		long framed = FrameSurface(plan);
		foreach (WorldCompilerRoom room in plan.Structures.Rooms)
			framed += FrameArea(plan, room.Bounds);
		foreach (WorldCompilerDecoration entry in plan.Decorations.Entries)
			framed += FrameArea(plan, entry.Bounds);
		foreach (WorldCompilerCorridor corridor in plan.Structures.Corridors)
		{
			WorldCompilerRoom from = plan.Structures.Rooms[corridor.From];
			WorldCompilerRoom to = plan.Structures.Rooms[corridor.To];
			int half = Math.Max(2, corridor.Width / 2);
			framed += FrameArea(plan, new(Math.Min(from.CenterX, to.CenterX) - half,
				from.CenterY - half, Math.Abs(from.CenterX - to.CenterX) + half * 2 + 1, half * 2 + 1));
			framed += FrameArea(plan, new(to.CenterX - half, Math.Min(from.CenterY, to.CenterY) - half,
				half * 2 + 1, Math.Abs(from.CenterY - to.CenterY) + half * 2 + 1));
		}
		foreach (WorldCompilerAccess access in WorldCompilerAccessPlan.Create(plan.Surface, plan.Structures.Rooms))
		{
			framed += FrameArea(plan, access.Shaft);
			framed += FrameArea(plan, access.Hall);
		}
		WorldCompilerSurfaceReceipt surface = Validate(plan);
		global::DingDongBiomeHomes.DingDongBiomeHomes.Instance.Logger.Info(
			$"DINGDONG_WORLD_COMPILER_SURFACE|verdict=PASS" +
			$"|samples={surface.Samples}|missing={surface.Missing}" +
			$"|maximumDrift={surface.MaximumDrift}");
		return framed;
	}

	private static long FrameSurface(WorldCompilerPlan plan)
	{
		long framed = 0;
		for (int x = 1; x < plan.Width - 1; x++)
		{
			int top = Math.Max(1, plan.Surface[x] - 12);
			int bottom = Math.Min(plan.Height - 2, plan.Surface[x] + 18);
			for (int y = top; y <= bottom; y++) framed += Frame(x, y);
		}
		return framed;
	}

	private static long FrameArea(WorldCompilerPlan plan, WorldCompilerRect area)
	{
		long framed = 0;
		int left = Math.Max(1, area.X - 1);
		int right = Math.Min(plan.Width - 2, area.Right + 1);
		int top = Math.Max(1, area.Y - 1);
		int bottom = Math.Min(plan.Height - 2, area.Bottom + 1);
		for (int x = left; x <= right; x++)
		for (int y = top; y <= bottom; y++) framed += Frame(x, y);
		return framed;
	}

	private static long Frame(int x, int y)
	{
		Tile tile = Main.tile[x, y];
		if (!tile.HasTile && tile.WallType == WallID.None) return 0;
		if (tile.HasTile) WorldGen.SquareTileFrame(x, y, true);
		if (tile.WallType != WallID.None) WorldGen.SquareWallFrame(x, y, true);
		return 1;
	}

	private static WorldCompilerSurfaceReceipt Validate(WorldCompilerPlan plan)
	{
		if (Main.spawnTileX < 30 || Main.spawnTileX >= plan.Width - 30 ||
			Main.spawnTileY < 30 || Main.spawnTileY >= plan.Height - 30)
			throw new InvalidOperationException("Compiler spawn escaped safe bounds.");
		WorldCompilerSurfaceReceipt surface = WorldCompilerSurfaceContract.Survey(
			plan.Width, plan.Height, plan.Surface,
			static (x, y) => Main.tile[x, y].HasTile);
		if (!surface.Complete)
			throw new InvalidOperationException(
				$"Compiled surface coverage failed: missing={surface.Missing}/" +
				$"{surface.Samples}, firstX={surface.FirstMissingX}.");
		for (int x = Main.spawnTileX - 2; x <= Main.spawnTileX + 2; x++)
		for (int y = Main.spawnTileY - 3; y <= Main.spawnTileY + 2; y++)
			if (Main.tile[x, y].LiquidAmount != 0)
				throw new InvalidOperationException("Compiler spawn contains liquid.");
		if (Main.dungeonX <= 0 || Main.dungeonY <= 0)
			throw new InvalidOperationException("Compiler dungeon contract missing.");
		return surface;
	}
}
