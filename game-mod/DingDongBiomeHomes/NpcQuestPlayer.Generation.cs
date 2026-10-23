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
public sealed partial class NpcQuestPlayer
{
	internal void ReplaceCurrentSnapshot(IEnumerable<(string NpcKey, ResidentQuestProgress Progress)> records)
	{
		string worldKey = CurrentWorldKey();
		string prefix = worldKey + KeySeparator;
		foreach (string key in progressByWorldAndNpc.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray())
			progressByWorldAndNpc.Remove(key);
		foreach ((string npcKey, ResidentQuestProgress progress)in records)
		{
			if (NpcTownManifest.TryGet(npcKey, out _))
				progressByWorldAndNpc[ComposeKey(worldKey, npcKey)] = progress;
		}

		Touch();
	}
}
