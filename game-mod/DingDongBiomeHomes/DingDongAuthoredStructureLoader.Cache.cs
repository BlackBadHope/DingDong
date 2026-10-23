using System.IO;
using System.IO.Compression;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

public static partial class DingDongAuthoredStructureLoader
{
	private static StructureData GetOrLoad(Mod mod, string resourcePath)
	{
		if (Cache.TryGetValue(resourcePath, out StructureData cached))
			return cached;
		if (PublicHomeReplacementCatalog.IsForbiddenLegacyResource(resourcePath))
			throw new InvalidDataException("Public builds cannot load private home payloads; use a procedural public preset.");
		if (PublicHomeReplacementCatalog.IsProcedural(resourcePath))
		{
			StructureData generated = CreatePublicHome(resourcePath);
			Cache.Add(resourcePath, generated);
			return generated;
		}
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
