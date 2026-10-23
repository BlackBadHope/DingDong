using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Terraria;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static partial class WorldCompilerStructureRasterizer
{
	internal static WorldCompilerStructureReceipt Generate(WorldCompilerPlan plan)
	{
		var clock = Stopwatch.StartNew();
		long changed = 0;
		Parallel.For(0, plan.Chunks.Length,
			WorldCompilerExecutionPolicy.ParallelOptions, index =>
			{
				long local = RasterChunk(plan, plan.Chunks[index]);
				Interlocked.Add(ref changed, local);
			});
		WorldCompilerAccessRasterizer.Generate(plan);
		foreach (WorldCompilerRoom room in plan.Structures.Rooms)
			GenVars.structures.AddProtectedStructure(
				new Microsoft.Xna.Framework.Rectangle(room.Bounds.X, room.Bounds.Y,
					room.Bounds.Width, room.Bounds.Height), 12);
		foreach (WorldCompilerCorridor corridor in plan.Structures.Corridors)
		{
			WorldCompilerRoom from = plan.Structures.Rooms[corridor.From];
			WorldCompilerRoom to = plan.Structures.Rooms[corridor.To];
			int half = Math.Max(2, corridor.Width / 2);
			GenVars.structures.AddProtectedStructure(new Microsoft.Xna.Framework.Rectangle(
				Math.Min(from.CenterX, to.CenterX) - half, from.CenterY - half,
				Math.Abs(from.CenterX - to.CenterX) + half * 2 + 1, half * 2 + 1), 8);
			GenVars.structures.AddProtectedStructure(new Microsoft.Xna.Framework.Rectangle(
				to.CenterX - half, Math.Min(from.CenterY, to.CenterY) - half,
				half * 2 + 1, Math.Abs(from.CenterY - to.CenterY) + half * 2 + 1), 8);
		}
		return new WorldCompilerStructureReceipt(changed,
			plan.Structures.Rooms.Length, plan.Structures.Corridors.Length,
			clock.ElapsedMilliseconds);
	}

	private static long RasterChunk(WorldCompilerPlan plan, WorldCompilerChunk chunk)
	{
		long changed = 0;
		WorldCompilerRuntimeWorkIndex work = WorldCompilerRuntimeState.WorkIndex ??
			throw new InvalidOperationException("World compiler work index is unavailable.");
		foreach (int roomIndex in work.Rooms[chunk.Index])
		{
			WorldCompilerRoom room = plan.Structures.Rooms[roomIndex];
			changed += room.Kind switch
			{
				WorldCompilerStructureKind.JungleResidencePlot => 0,
				WorldCompilerStructureKind.FloatingIsland =>
					RasterIsland(chunk, room.Bounds),
				WorldCompilerStructureKind.DesertPyramid =>
					RasterPyramid(chunk, room.Bounds),
				WorldCompilerStructureKind.ResidenceCavern =>
					RasterResidenceCavern(chunk, room.Bounds, mushroom: false),
				WorldCompilerStructureKind.MushroomResidenceCavern =>
					RasterResidenceCavern(chunk, room.Bounds, mushroom: true),
				_ => RasterRoom(chunk, room)
			};
		}
		// Build every shell before opening any passage. Otherwise a later room
		// or intersecting corridor seals an earlier doorway or elbow.
		foreach (int corridorIndex in work.Corridors[chunk.Index])
			changed += RasterCorridor(plan, chunk,
				plan.Structures.Corridors[corridorIndex], false);
		foreach (int roomIndex in work.Rooms[chunk.Index])
		{
			WorldCompilerRoom room = plan.Structures.Rooms[roomIndex];
			if (room.Kind is WorldCompilerStructureKind.DungeonRoom or
				WorldCompilerStructureKind.TempleRoom)
				changed += OpenRoom(chunk, room);
		}
		foreach (int corridorIndex in work.Corridors[chunk.Index])
			changed += RasterCorridor(plan, chunk,
				plan.Structures.Corridors[corridorIndex], true);
		return changed;
	}
}
