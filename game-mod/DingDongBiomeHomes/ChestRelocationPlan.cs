using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;

namespace DingDongBiomeHomes;
public sealed record ChestRelocationPlan(int SourceIndex, Point16 SourceTopLeft, Point16 DestinationTopLeft, ushort TileType, int Style, int ItemCount, string ItemSignature);
