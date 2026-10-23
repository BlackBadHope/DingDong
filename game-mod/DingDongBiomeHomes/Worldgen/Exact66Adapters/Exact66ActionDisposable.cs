using System;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal sealed class Exact66ActionDisposable(Action action) : IDisposable
{
	private Action action = action;

	public void Dispose()
	{
		Action current = action;
		action = null;
		current?.Invoke();
	}
}
