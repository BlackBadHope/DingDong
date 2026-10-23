using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal sealed record StageObservation(string Key, bool Healthy, string Detail);

internal sealed class WorldgenStageJournal
{
	private readonly string path;
	private readonly Func<StageObservation[]> observe;
	private readonly Action<string> warn;
	private readonly Func<object> state;
	private readonly Dictionary<string, string> firstFailure = new();
	private int sequence;
	private HashSet<string> previousBroken = new();
	internal WorldgenStageJournal(string path, object metadata,
		Func<StageObservation[]> observe, Action<string> warn,
		string coverage = "registered progression objects, chest slots, obligations, altar anchor, planned passages; not a whole-world tile audit",
		Func<object> state = null)
	{
		this.path = path; this.observe = observe; this.warn = warn; this.state = state;
		Directory.CreateDirectory(Path.GetDirectoryName(path));
		using (new FileStream(path, FileMode.CreateNew, FileAccess.Write)) { }
		Write(new { schema = 2, kind = "RUN", utc = DateTime.UtcNow, metadata,
			coverage });
	}

	internal void Run(string stage, Action action)
	{
		int id = ++sequence;
		var before = Capture();
		Record(id, stage, "START", 0, before, null);
		var clock = Stopwatch.StartNew();
		try { action(); }
		catch (Exception error)
		{
			// Never mask the original worldgen exception with a diagnostic I/O failure.
			try { Record(id, stage, "ERROR", clock.ElapsedMilliseconds, Capture(), error.ToString()); }
			catch (Exception journalError) { warn($"Stage journal write failed: {journalError}"); }
			throw;
		}
		Record(id, stage, "END", clock.ElapsedMilliseconds, Capture(), null);
	}

	private StageObservation[] Capture()
	{
		try { return observe(); }
		catch (Exception error) { return new[] { new StageObservation("snapshot-reader", false, error.ToString()) }; }
	}

	private void Record(int id, string stage, string kind, long elapsedMs,
		StageObservation[] snapshot, string exception)
	{
		var broken = snapshot.Where(s => !s.Healthy).ToArray();
		foreach (var item in broken)
			if (firstFailure.TryAdd(item.Key, $"{id}:{stage}:{kind}"))
				warn($"DINGDONG_STAGE_FIRST_LOSS|stage={stage}|boundary={kind}|object={item.Key}|detail={item.Detail}");
		var current = broken.Select(s => s.Key).ToHashSet();
		var newlyBroken = current.Except(previousBroken).ToArray();
		var recovered = previousBroken.Except(current).Where(key => snapshot.Any(s => s.Key == key && s.Healthy)).ToArray();
		var unobserved = previousBroken.Except(current).Except(recovered).ToArray();
		object context;
		try { context = state?.Invoke(); }
		catch (Exception error) { context = new { captureError = error.ToString() }; }
		Write(new { schema = 2, kind, id, stage, utc = DateTime.UtcNow, elapsedMs, context,
			integrityHealthy = broken.Length == 0, checkedCount = snapshot.Length,
			brokenCount = broken.Length, newlyBroken, recovered, unobserved,
			exception, failures = broken, snapshot, firstFailure });
		previousBroken = current;
	}

	private void Write(object value)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value) + "\n");
		using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
		stream.Write(bytes);
		stream.Flush(flushToDisk: true);
	}
}
