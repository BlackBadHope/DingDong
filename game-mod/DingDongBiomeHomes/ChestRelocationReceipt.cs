using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;

namespace DingDongBiomeHomes;
public sealed record ChestRelocationReceipt(Point16 SourceTopLeft, Point16 DestinationTopLeft, int DestinationIndex, int ItemCount, string ItemSignature, bool ReusedSourceSlot);
