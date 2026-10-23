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
	private static bool TryResolveNpcType(string npcKey, out int npcType)
	{
		const string vanillaPrefix = "Terraria/";
		if (npcKey.StartsWith(vanillaPrefix, StringComparison.Ordinal))
			return NPCID.Search.TryGetId(npcKey[vanillaPrefix.Length..], out npcType);
		if (ModContent.TryFind(npcKey, out ModNPC modNpc))
		{
			npcType = modNpc.Type;
			return true;
		}

		npcType = NPCID.None;
		return false;
	}
}
