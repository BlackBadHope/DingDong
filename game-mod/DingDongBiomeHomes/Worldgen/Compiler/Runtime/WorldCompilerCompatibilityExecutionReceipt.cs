using System;
using System.Collections.Generic;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal sealed class WorldCompilerCompatibilityExecutionReceipt
{
	private readonly string[] names;
	private readonly bool[] completed;

	internal int Expected => names.Length;
	internal int Completed { get; private set; }
	internal ulong Fingerprint { get; }
	internal bool Complete => Expected > 0 && Completed == Expected;

	internal WorldCompilerCompatibilityExecutionReceipt(IReadOnlyList<GenPass> passes)
	{
		if (passes == null || passes.Count == 0)
			throw new ArgumentException("Compatibility pass set is empty.", nameof(passes));
		names = new string[passes.Count];
		completed = new bool[passes.Count];
		ulong fingerprint = 1469598103934665603UL;
		for (int index = 0; index < passes.Count; index++)
		{
			string name = passes[index]?.Name;
			if (string.IsNullOrWhiteSpace(name))
				throw new InvalidOperationException(
					$"Compatibility pass {index} has no name.");
			names[index] = name;
			foreach (char value in name)
			{
				fingerprint ^= value;
				fingerprint *= 1099511628211UL;
			}
		}
		Fingerprint = fingerprint;
	}

	internal void MarkCompleted(int index, string name)
	{
		if ((uint)index >= (uint)names.Length)
			throw new ArgumentOutOfRangeException(nameof(index));
		if (!string.Equals(names[index], name, StringComparison.Ordinal))
			throw new InvalidOperationException(
				$"Compatibility pass identity changed at {index}: {name} != {names[index]}.");
		if (completed[index])
			throw new InvalidOperationException(
				$"Compatibility pass completed twice: {name}.");
		completed[index] = true;
		Completed++;
	}
}
