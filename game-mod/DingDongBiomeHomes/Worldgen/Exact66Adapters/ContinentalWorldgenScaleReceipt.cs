namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static class ContinentalWorldgenScaleReceipt
{
	internal static bool BuffersReady { get; private set; }
	internal static ContinentalVanillaBufferPlan Buffers { get; private set; }
	internal static bool AbyssReady { get; private set; }
	internal static int AbyssCapacity { get; private set; }
	internal static bool Complete => BuffersReady && AbyssReady;
	internal static string Value => BuffersReady && AbyssReady
		? $"scale:buffers={Buffers.LarvaCapacity}/{Buffers.JungleChestCapacity}/" +
			$"{Buffers.SkyFeatureCapacity}/{Buffers.MountainCaveCapacity}/" +
			$"{Buffers.EvilHeartCapacity},abyss={AbyssCapacity}"
		: "scale:buffers=pending";

	internal static void MarkBuffers(ContinentalVanillaBufferPlan plan)
	{
		Buffers = plan;
		BuffersReady = true;
	}

	internal static void MarkAbyss(int capacity)
	{
		AbyssCapacity = capacity;
		AbyssReady = true;
	}

	internal static void Reset()
	{
		BuffersReady = false;
		AbyssReady = false;
		AbyssCapacity = 0;
		Buffers = default;
	}
}
