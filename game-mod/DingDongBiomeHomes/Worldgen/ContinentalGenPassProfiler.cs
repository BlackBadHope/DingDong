using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen;

internal static class ContinentalGenPassProfiler
{
	private const double SlowMilliseconds = 500d;
	private static readonly List<PassSample> Samples = [];

	internal static void Wrap(List<GenPass> tasks, Action<string> log)
	{
		Samples.Clear();
		for (int i = 0; i < tasks.Count; i++)
		{
			GenPass original = tasks[i];
			if (original is TimedGenPass) continue;
			TimedGenPass timed = new(original, log);
			if (!original.Enabled) timed.Disable();
			tasks[i] = timed;
		}
	}

	internal static void WriteSummary(Action<string> log)
	{
		PassSample[] ordered = Samples.OrderByDescending(sample => sample.Milliseconds)
			.Take(20).ToArray();
		double total = Samples.Sum(sample => sample.Milliseconds);
		string top = string.Join(',', ordered.Select(sample =>
			$"{Safe(sample.Name)}:{sample.Milliseconds:F0}ms" +
			$"/ws={sample.WorkingSetMiB}MiB/private={sample.PrivateMiB}MiB" +
			$"/gc={sample.GcCommittedMiB}MiB"));
		log($"DINGDONG_WORLDGEN_PASS_PROFILE|verdict=PASS|passes={Samples.Count}|" +
			$"totalMs={total:F0}|top={top}");
	}

	private static void Record(PassSample sample)
	{
		Samples.Add(sample);
	}

	private static string Safe(string value) => value.Replace('|', '_').Replace(',', '_');

	private readonly record struct PassSample(string Name, double Milliseconds,
		long WorkingSetMiB, long PrivateMiB, long ManagedMiB,
		long HeapMiB, long GcCommittedMiB, int Gen0, int Gen1, int Gen2);

	private sealed class TimedGenPass : GenPass
	{
		private readonly GenPass original;
		private readonly Action<string> log;

		internal TimedGenPass(GenPass original, Action<string> log)
			: base(original.Name, original.Weight)
		{
			this.original = original;
			this.log = log;
		}

		protected override void ApplyPass(GenerationProgress progress,
			GameConfiguration configuration)
		{
			long started = Stopwatch.GetTimestamp();
			int gen0 = GC.CollectionCount(0), gen1 = GC.CollectionCount(1), gen2 = GC.CollectionCount(2);
			try { original.Apply(progress, configuration); }
			finally
			{
				double elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
				const long MiB = 1024L * 1024L;
				using Process process = Process.GetCurrentProcess();
				process.Refresh();
				GCMemoryInfo gc = GC.GetGCMemoryInfo();
				long memory = process.WorkingSet64 / MiB;
				PassSample sample = new(Name, elapsed, memory,
					process.PrivateMemorySize64 / MiB, GC.GetTotalMemory(false) / MiB,
					gc.HeapSizeBytes / MiB, gc.TotalCommittedBytes / MiB,
					GC.CollectionCount(0) - gen0, GC.CollectionCount(1) - gen1,
					GC.CollectionCount(2) - gen2);
				Record(sample);
				if (elapsed >= SlowMilliseconds)
					log($"DINGDONG_WORLDGEN_PASS_SLOW|name={Safe(Name)}|ms={elapsed:F0}|" +
						$"workingSetMiB={memory}|privateMiB={sample.PrivateMiB}|" +
						$"managedMiB={sample.ManagedMiB}|heapMiB={sample.HeapMiB}|" +
						$"gcCommittedMiB={sample.GcCommittedMiB}|" +
						$"gc={sample.Gen0}/{sample.Gen1}/{sample.Gen2}");
			}
		}
	}
}
