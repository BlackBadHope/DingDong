using System;
using System.Runtime;

namespace DingDongBiomeHomes.Worldgen;

// World generation is not interactive gameplay. Scope Batch GC to that work,
// restoring the caller's mode even when the generator throws.
internal sealed class WorldgenThroughputScope : IDisposable
{
	private readonly GCLatencyMode previous;
	private readonly bool changed;
	private bool disposed;

	internal WorldgenThroughputScope(bool enabled, Action<string> warning = null)
	{
		previous = GCSettings.LatencyMode;
		if (!enabled || previous is GCLatencyMode.Batch or GCLatencyMode.NoGCRegion) return;
		try
		{
			GCSettings.LatencyMode = GCLatencyMode.Batch;
			changed = true;
		}
		catch (InvalidOperationException ex) { warning?.Invoke(ex.Message); }
	}

	public void Dispose()
	{
		if (disposed) return;
		disposed = true;
		if (changed && GCSettings.LatencyMode == GCLatencyMode.Batch)
			GCSettings.LatencyMode = previous;
	}
}
