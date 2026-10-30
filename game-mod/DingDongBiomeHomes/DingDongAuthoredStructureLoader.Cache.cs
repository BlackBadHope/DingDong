using System.IO;
using System.IO.Compression;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

public static partial class DingDongAuthoredStructureLoader
{
	// RamOpt 28.10: access counters for the cache receipt. They show whether a
	// byte-budgeted (LRU) cache would re-decode rarely or on every request.
	private static int cacheHits, cacheLoads;

	private static readonly object cacheLock = new();

	// Candidate pools are collected on several threads; one structure decodes once.
	private static StructureData GetOrLoad(Mod mod, string resourcePath)
	{
		lock (cacheLock) return GetOrLoadLocked(mod, resourcePath);
	}

	private static StructureData GetOrLoadLocked(Mod mod, string resourcePath)
	{
		if (Cache.TryGetValue(resourcePath, out StructureData cached))
		{
			cacheHits++;
			return cached;
		}
		cacheLoads++;
		if (resourcePath == AuthoredSourceContext.JungleResource)
		{
			using Stream reviewed = mod.GetFileStream(resourcePath);
			string digest = System.Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(reviewed));
			if (digest != AuthoredSourceContext.JungleSha256)
				throw new InvalidDataException("Restored source context changed without a reviewed container/anchor contract.");
		}

		using Stream source = mod.GetFileStream(resourcePath);
		using GZipStream gzip = new(source, CompressionMode.Decompress);
		using BinaryReader reader = new(gzip);
		StructureData loaded = Read(reader, resourcePath);
		Cache.Add(resourcePath, loaded);
		return loaded;
	}
}
