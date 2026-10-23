using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes.Worldgen;

internal sealed class ContinentalLiquidHookSet : IDisposable
{
	private readonly List<IDisposable> hooks = new();
	internal void Add(Func<IDisposable> create)
	{
		try { hooks.Add(create()); }
		catch { Dispose(); throw; }
	}
	public void Dispose()
	{
		Exception failure = null;
		for (int i = hooks.Count - 1; i >= 0; i--)
		{
			try { hooks[i].Dispose(); }
			catch (Exception error) { failure ??= error; }
		}
		hooks.Clear();
		if (failure != null)
			throw new InvalidOperationException("Liquid hook cleanup attempted all handles but one failed.", failure);
	}
}
