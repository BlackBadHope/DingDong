using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
/// <summary>
/// Runtime-independent reader for the reviewed Structure Helper 3.0.1/3.1.0
/// vanilla subset used by DingDong. It intentionally rejects NBT, custom
/// tile-data blocks and arbitrary modded tile/wall mappings instead of loading
/// another assembly.
/// </summary>
public static partial class DingDongAuthoredStructureLoader
{
	private const string Header = "STRUCTURE_HELPER_STRUCTURE";
	private const string NullTileKey = "StructureHelper/Content/Tiles/NullBlock";
	private const string NullWallKey = "StructureHelper/Content/Tiles/NullWall";
	private static readonly HashSet<string> SupportedVersions = new(StringComparer.Ordinal)
	{
		"3.0.1",
		"3.1.0"
	};
	private static readonly Dictionary<string, StructureData> Cache = new(StringComparer.Ordinal);
	private static readonly Dictionary<string, StructureWritePlan> IntegratedWritePlanCache = new(StringComparer.Ordinal);
	private static readonly Dictionary<string, StructureWritePlan> RawWritePlanCache = new(StringComparer.Ordinal);
}
