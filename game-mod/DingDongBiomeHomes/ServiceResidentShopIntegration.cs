using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;

namespace DingDongBiomeHomes;
internal static partial class ServiceResidentShopIntegration
{
	public const string ShopName = "DingDongHomeGoods";
	private const string HomeGoodsButtonKey = "Mods.DingDongBiomeHomes.NpcDialogue.HomeGoodsButton";
	private const string GoblinFusionButtonKey = "Mods.DingDongBiomeHomes.NpcDialogue.GoblinFusionButton";
	private static readonly Dictionary<int, NpcTownIdentity> ServiceResidentsByType = new();
}
