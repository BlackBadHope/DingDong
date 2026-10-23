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
public sealed partial class NpcQuestWorldLedger : ModSystem
{
	private const char Separator = '\u001f';
	private const int SaveVersion = 4;
	private static readonly Dictionary<string, ResidentQuestProgress> Records = new(StringComparer.Ordinal);
}
