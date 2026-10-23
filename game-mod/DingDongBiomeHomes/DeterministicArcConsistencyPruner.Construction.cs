using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;
/// <summary>
/// Bounded deterministic AC-3 pruning for already narrowed candidate domains.
/// A budget stop preserves only revisions backed by complete no-support proofs.
/// </summary>
public static partial class DeterministicArcConsistencyPruner
{
	private static void Enqueue(int source, int support, Queue<(int Source, int Support)> queue, bool[, ] queued)
	{
		if (queued[source, support])
			return;
		queued[source, support] = true;
		queue.Enqueue((source, support));
	}
}
