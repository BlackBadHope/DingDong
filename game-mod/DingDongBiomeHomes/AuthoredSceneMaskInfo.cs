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
public readonly record struct AuthoredSceneMaskInfo(int Width, int Height, int AuthoredContentCells, int InteriorVoidCells, int BoundaryConnectedExteriorCells)
{
	public int TotalCells => checked(Width * Height);
}
