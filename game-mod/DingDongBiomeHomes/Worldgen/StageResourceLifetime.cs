using System;

namespace DingDongBiomeHomes.Worldgen;

// Generation/diagnostic consumers are synchronous. Nested consumers must not
// release resources belonging to their outer planning/placement transaction.
internal sealed class StageResourceLifetime(Action release)
{
	internal int Consumers { get; private set; }
	internal IDisposable Enter()
	{
		Consumers++;
		return new Lease(this);
	}

	internal bool ReleaseWhenIdle()
	{
		if (Consumers != 0) return false;
		release();
		return true;
	}

	private sealed class Lease(StageResourceLifetime owner) : IDisposable
	{
		public void Dispose()
		{
			if (owner == null) return;
			StageResourceLifetime current = owner;
			owner = null;
			if (--current.Consumers == 0) current.ReleaseWhenIdle();
		}
	}
}
