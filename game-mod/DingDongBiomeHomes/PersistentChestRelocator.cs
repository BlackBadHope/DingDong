using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;

namespace DingDongBiomeHomes;
/// <summary>
/// Lossless chest relocation for dedicated probes and authored worldgen. The
/// existing chest index is rebound to a verified destination before source
/// tiles are cleared, so full modded chest arrays remain safe.
/// </summary>
public static partial class PersistentChestRelocator
{
	private const int ObjectClearance = 8;
	// 30.10: an island house in the open ocean is up to ~4500 tiles from dry land; 1200 left its
	// displaced chest without a destination and failed the whole layout (owner seed, S25).
	private const int MaxHorizontalSearch = 4800;
	private const int MaxVerticalSearch = 260;
}
