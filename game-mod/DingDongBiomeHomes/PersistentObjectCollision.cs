using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
public sealed record PersistentObjectCollision(Point16 Position, string Kind)
{
	public override string ToString() => $"{Kind}@{Position.X},{Position.Y}";
}
