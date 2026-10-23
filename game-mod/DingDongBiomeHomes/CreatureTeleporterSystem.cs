using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
public sealed partial class CreatureTeleporterSystem : ModSystem
{
	private const ulong TeleportCooldownTicks = 90;
	private static readonly HashSet<Point16> PulsePlatforms = new();
	private static readonly Dictionary<int, ulong> CooldownByNpc = new();
	private static ulong pulseTick = ulong.MaxValue;
	private static ulong processedTick = ulong.MaxValue;
}
