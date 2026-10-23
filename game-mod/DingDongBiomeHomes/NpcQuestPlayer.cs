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
public sealed partial class NpcQuestPlayer : ModPlayer
{
	private const char KeySeparator = '\u001f';
	private const ulong IdentityRetryTicks = 120;
	private readonly Dictionary<string, ResidentQuestProgress> progressByWorldAndNpc = new(StringComparer.Ordinal);
	private bool identityConfirmed;
	private ulong nextIdentityRetryTick;
	internal string PersistentId { get; private set; } = string.Empty;
	internal bool IdentityConfirmed => identityConfirmed;
	internal uint Revision { get; private set; }
}
