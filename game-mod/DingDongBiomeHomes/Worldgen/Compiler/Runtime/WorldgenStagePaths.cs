using System.Collections.Generic;
using System.Linq;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static class WorldgenStagePaths
{
	internal static void Append(List<StageObservation> result)
	{
		var plan = WorldCompilerRuntimeState.Current;
		if (plan == null) { result.Add(new("paths:coverage", WorldCompilerRuntimeState.Receipt?.Complete == true, "plan unavailable; paths not rechecked after release")); return; }
		var edges = new List<(int, int)>();
		foreach (var corridor in plan.Structures.Corridors)
		{
			var from = plan.Structures.Rooms[corridor.From];
			var to = plan.Structures.Rooms[corridor.To];
			if (from.Kind is not (WorldCompilerStructureKind.DungeonRoom or WorldCompilerStructureKind.TempleRoom)) continue;
			edges.Add((from.Id, to.Id));
			ReadPath(result, $"path:corridor:{from.Id}:{to.Id}", new(from.CenterX, from.CenterY, to.CenterX, to.CenterY));
		}
		foreach (var access in WorldCompilerAccessPlan.Create(plan.Surface, plan.Structures.Rooms))
		{
			var room = plan.Structures.Rooms[access.RoomId];
			var nodes = plan.Structures.Rooms.Where(r => r.Kind == room.Kind).Select(r => r.Id).ToList();
			string key = $"graph:{room.Kind}:{room.Id}";
			WorldgenStageContracts.Check(result, key, () => new(key,
				WorldCompilerPathContract.Connected(room.Id, nodes, edges), $"root={room.Id};nodes={nodes.Count}"));
			ReadPath(result, $"path:access:{room.Id}", new(access.X, access.Top, access.TargetX, access.Bottom, true));
		}
	}

	private static void ReadPath(List<StageObservation> result, string key, WorldCompilerWalkPath path) =>
		WorldgenStageContracts.Check(result, key, () => {
			var blocked = WorldgenPathDiagnostics.Blocked(path, WorldCompilerProgressionValidation.Passable);
			return new(key, blocked.Count == 0, $"blockedCount={blocked.Count};blocked=" +
				string.Join(";", blocked.Select(p => $"{p.X},{p.Y}")));
		});
}
