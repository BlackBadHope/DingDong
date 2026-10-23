using System;
using System.Reflection;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static partial class SotsGemPlacement
{
	private static Action<int, int> generateTopaz;
	private static Action<int, int> generateSapphire;

	internal static void Bind(MethodInfo topaz, MethodInfo sapphire)
	{
		generateTopaz = topaz.CreateDelegate<Action<int, int>>();
		generateSapphire = sapphire.CreateDelegate<Action<int, int>>();
	}

	internal static void Reset()
	{
		generateTopaz = null;
		generateSapphire = null;
	}

	private static void GenerateTopaz(int x, int y) =>
		(generateTopaz ?? throw new InvalidOperationException("SOTS Topaz generator is unbound."))(x, y);

	private static void GenerateSapphire(int x, int y) =>
		(generateSapphire ?? throw new InvalidOperationException("SOTS Sapphire generator is unbound."))(x, y);
}
