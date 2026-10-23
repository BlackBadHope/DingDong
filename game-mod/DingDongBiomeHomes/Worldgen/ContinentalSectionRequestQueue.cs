using System.Collections.Generic;

namespace DingDongBiomeHomes.Worldgen;

internal sealed class ContinentalSectionRequestQueue
{
	internal const int MaximumPending = 8;
	private const double LifetimeSeconds = 10;
	private readonly List<Request> pending = new(MaximumPending);
	internal int Count => pending.Count;

	internal bool Enqueue(int x, int y, bool urgent, double now)
	{
		Prune(now);
		for (int i = 0; i < pending.Count; i++)
		{
			Request previous = pending[i];
			if (previous.X != x || previous.Y != y) continue;
			// A freshly admitted retry/upgrade renews interest in this section.
			pending[i] = previous with { Urgent = urgent || previous.Urgent, CreatedAt = now };
			return true;
		}
		if (pending.Count >= MaximumPending)
		{
			if (!urgent) return false;
			int replace = pending.FindIndex(request => !request.Urgent);
			if (replace < 0) return false;
			pending.RemoveAt(replace);
		}
		pending.Add(new(x, y, urgent, now));
		return true;
	}

	internal bool TryDequeue(bool urgent, double now, out Request request)
	{
		Prune(now);
		int index = pending.FindIndex(candidate => candidate.Urgent == urgent);
		if (index < 0) { request = default; return false; }
		request = pending[index];
		pending.RemoveAt(index);
		return true;
	}

	internal void Requeue(Request request)
	{
		// The caller has just removed this entry. Preserve its original lifetime
		// without pruning other, newer entries against an old timestamp.
		if (pending.Count < MaximumPending) pending.Add(request);
	}

	private void Prune(double now) => pending.RemoveAll(request =>
		now < request.CreatedAt || now - request.CreatedAt >= LifetimeSeconds);

	internal readonly record struct Request(int X, int Y, bool Urgent, double CreatedAt);
}
