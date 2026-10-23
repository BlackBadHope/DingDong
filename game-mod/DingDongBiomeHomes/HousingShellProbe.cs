using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
public readonly record struct HousingShellProbe(Rectangle Bounds, Point CheckPoint, Point HomePoint, int ChairTiles, int TableTiles, int TorchTiles, int DoorTiles, bool RoomNeeds);
