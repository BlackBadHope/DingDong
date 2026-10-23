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
public sealed partial class SpawnSanctuaryWorldgenSystem : ModSystem
{
	private const double PassWeight = 8d;
	private const int MinimumFoundationHalfWidth = 16;
	private const int MaximumFoundationHalfWidth = 28;
	private const int FoundationMargin = 6;
	private const int FoundationDepth = 6;
	private const int SpawnPocketHeight = 7;
	private const int SpawnPocketHalfWidth = 3;
	private const int SillWidth = 3;
	private const int SillHeight = 4;
	private static readonly string[] VariantIds =
	{
		"SmallHouse1",
		"MediumHouse1",
		"Greenhouse1"
	};
	private SpawnSanctuaryReceipt generatedSanctuary;
}
