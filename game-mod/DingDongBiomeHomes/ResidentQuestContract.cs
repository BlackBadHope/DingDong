using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;
internal readonly record struct ResidentQuestContract(int SupplyItemType, int SupplyCount, int DefenseKills)
{
	internal static ResidentQuestContract For(NpcTownIdentity identity) => identity.Biome switch
	{
		HomeBiome.Forest => new ResidentQuestContract(ItemID.Wood, 25, 8),
		HomeBiome.Snow => new ResidentQuestContract(ItemID.BorealWood, 25, 8),
		HomeBiome.Desert => new ResidentQuestContract(ItemID.SandBlock, 40, 8),
		HomeBiome.Jungle => new ResidentQuestContract(ItemID.RichMahogany, 25, 8),
		HomeBiome.Ocean => new ResidentQuestContract(ItemID.Coral, 5, 8),
		HomeBiome.Underground => new ResidentQuestContract(ItemID.StoneBlock, 40, 8),
		HomeBiome.Hallow => new ResidentQuestContract(ItemID.FallenStar, 3, 8),
		HomeBiome.Mushroom => new ResidentQuestContract(ItemID.GlowingMushroom, 15, 8),
		HomeBiome.Underworld => new ResidentQuestContract(ItemID.AshBlock, 40, 8),
		_ => new ResidentQuestContract(ItemID.Wood, 25, 8)};
}
