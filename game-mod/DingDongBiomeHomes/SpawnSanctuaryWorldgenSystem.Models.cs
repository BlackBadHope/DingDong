using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Generation;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes;
/// <summary>
/// Adds one seed-selected authored Structure Helper house to the world spawn.
/// A compact stone foundation, low lava sills and a final PostWorldGen drain
/// keep FTW/other generator passes from leaving the player inside lava without
/// turning the whole visible spawn into an arena-sized black box.
/// </summary>
public sealed partial class SpawnSanctuaryWorldgenSystem
{
	private sealed record SpawnSanctuaryReceipt(string VariantId, Rectangle AssetArea, Rectangle SafeArea, int SpawnX, int SpawnY, int FloorY, int LiquidCellsCleared);
}
