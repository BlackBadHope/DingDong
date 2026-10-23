#nullable enable
using System;
using System.Diagnostics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static partial class CalamityWorldgenAdapter
{
	private static Stopwatch? dungeonStageClock;
	private static long dungeonStageLastMs;
	private static string dungeonStageCurrent = string.Empty;

	private static void EmitDungeonStage(ILCursor cursor, string stage)
	{
		cursor.Emit(OpCodes.Ldstr, stage);
		cursor.EmitDelegate<Action<string>>(MarkDungeonStage);
	}

	private static void BeginDungeonStageProfile()
	{
		if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(
			"DINGDONG_X10_RUNTIME_RUN_ID"))) return;
		dungeonStageClock = Stopwatch.StartNew();
		dungeonStageLastMs = 0;
		dungeonStageCurrent = string.Empty;
		WriteDungeonStage("start", 0, 0);
	}

	private static void MarkDungeonStage(string stage)
	{
		if (dungeonStageClock is null) return;
		if (stage == dungeonStageCurrent) return;
		long total = dungeonStageClock.ElapsedMilliseconds;
		WriteDungeonStage(stage, total - dungeonStageLastMs, total);
		dungeonStageLastMs = total;
		dungeonStageCurrent = stage;
	}

	private static void FinishDungeonStageProfile()
	{
		if (dungeonStageClock is null) return;
		long total = dungeonStageClock.ElapsedMilliseconds;
		WriteDungeonStage("end", total - dungeonStageLastMs, total);
		dungeonStageClock = null;
		dungeonStageCurrent = string.Empty;
	}

	private static void WriteDungeonStage(string stage, long delta, long total) =>
		Console.WriteLine($"DINGDONG_DUNGEON_STAGE|stage={stage}|deltaMs={delta}|" +
			$"totalMs={total}|rooms={GenVars.numDRooms}|" +
			$"bounds={GenVars.dMinX},{GenVars.dMinY},{GenVars.dMaxX},{GenVars.dMaxY}");
}
