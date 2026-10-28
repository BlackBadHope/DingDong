using System;
using System.Collections.Generic;
using Terraria.ModLoader;
using DingDongBiomeHomes.Worldgen;

namespace DingDongBiomeHomes;

public static partial class DingDongAuthoredStructureLoader
{
	internal readonly record struct CacheUsage(int Structures, int Plans, long PayloadBytes);
	private static readonly StageResourceLifetime CacheLifetime = new(ClearTransientCaches);
	internal static CacheUsage LastReleasedCache { get; private set; }
	internal static IDisposable UseTransientCaches() => CacheLifetime.Enter();

	internal static CacheUsage InspectCacheUsage()
	{
		// Masks may retain these arrays after eviction. Count distinct cache
		// payload, not process RAM or guaranteed reclaimed bytes.
		var arrays = new HashSet<Array>(ReferenceEqualityComparer.Instance);
		foreach (StructureData data in Cache.Values)
		{
			arrays.Add(data.TileTypes); arrays.Add(data.WallTypes);
			arrays.Add(data.Liquids); arrays.Add(data.BrightnessAndInvisibility);
			arrays.Add(data.WireState);
		}
		foreach (StructureWritePlan plan in IntegratedWritePlanCache.Values)
		{
			arrays.Add(plan.WriteTiles); arrays.Add(plan.WriteWalls); arrays.Add(plan.MutationCells);
		}
		foreach (StructureWritePlan plan in RawWritePlanCache.Values)
		{
			arrays.Add(plan.WriteTiles); arrays.Add(plan.WriteWalls); arrays.Add(plan.MutationCells);
		}
		foreach (var entry in liquidSafetyEnvelopes) arrays.Add(entry.Value.SourceFlags);
		long bytes = 0;
		foreach (Array array in arrays) bytes += Buffer.ByteLength(array);
		return new(Cache.Count, IntegratedWritePlanCache.Count + RawWritePlanCache.Count, bytes);
	}

	private static int lastCacheHits, lastCacheLoads; // RamOpt 28.10

	private static void ClearTransientCaches()
	{
		LastReleasedCache = InspectCacheUsage();
		lastCacheHits = cacheHits; lastCacheLoads = cacheLoads; // RamOpt 28.10
		cacheHits = cacheLoads = 0;
		// Evict references; never clear shared array contents or foreign caches.
		Cache.Clear();
		IntegratedWritePlanCache.Clear();
		RawWritePlanCache.Clear();
		liquidSafetyEnvelopes.Clear();
		ClearSceneGroundDropCache(); // item 6: one-entry write-subset cache
	}

	internal static void ReleaseIdleCaches(Mod mod, string stage)
	{
		if (CacheLifetime.ReleaseWhenIdle()) ReportCacheRelease(mod, stage);
		lastCacheHits = lastCacheLoads = 0; // RamOpt 28.10: report each release once.
	}

	internal static void ReportCacheRelease(Mod mod, string stage)
	{
		CacheUsage current = InspectCacheUsage();
		mod.Logger.Info($"DINGDONG_TRANSIENT_CACHE|stage={stage}|structures={LastReleasedCache.Structures}" +
			$"|plans={LastReleasedCache.Plans}|evictedPayloadBytes={LastReleasedCache.PayloadBytes}" +
			$"|remainingBytes={current.PayloadBytes}|activeConsumers={CacheLifetime.Consumers}" +
			"|forcedGC=False|foreignBuffersTouched=False|reclaimedRamNotMeasured=True" +
			$"|cacheHits={lastCacheHits}|cacheLoads={lastCacheLoads}"); // RamOpt 28.10
	}
}
