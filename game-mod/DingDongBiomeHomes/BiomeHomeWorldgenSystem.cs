using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Generation;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes;
public sealed partial class BiomeHomeWorldgenSystem : ModSystem
{
	private const double PassWeight = 120d;
	private const string WorldgenTestProfileEnvironmentVariable = "DINGDONG_WORLDGEN_TEST_PROFILE";
	private const string WorldgenTestEvilEnvironmentVariable = "DINGDONG_WORLDGEN_TEST_EVIL";
}
