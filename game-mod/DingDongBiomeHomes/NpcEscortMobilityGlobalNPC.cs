using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
public sealed partial class NpcEscortMobilityGlobalNPC : GlobalNPC
{
	private const int ReplanInterval = 24;
	private const int StuckReplanTicks = 36;
	private const int StuckJumpTicks = 72;
	private const int StuckWaitTicks = 150;
	private readonly List<Point> path = new();
	private int waypointIndex;
	private int replanTimer;
	private int stuckTicks;
	private int vanillaStuckTicks;
	private ulong nextDestinationRetryTick;
	private Vector2 lastPosition;
	public override bool InstancePerEntity => true;
}
