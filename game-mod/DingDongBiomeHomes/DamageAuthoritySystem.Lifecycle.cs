using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
/// <summary>
/// Applies deterministic client prediction and validates the same decision on
/// the server before the vanilla DamageNPC packet is allowed to mutate life.
/// </summary>
public sealed partial class DamageAuthoritySystem
{
	public override void PreUpdateWorld()
	{
		if (Main.netMode == NetmodeID.Server)
			PruneApprovals((long)Main.GameUpdateCount);
		else if (Main.netMode == NetmodeID.MultiplayerClient)
			PruneClientCommits((long)Main.GameUpdateCount);
	}

	private static void ResetSession()
	{
		ClientLedger.Reset();
		ServerWorldLedger.Reset();
		ServerPlayerLedgers.Clear();
		PendingApprovals.Clear();
		ClientPendingCommits.Clear();
		LatestClientTick.Clear();
		serverAllocator = new ServerAttackInstanceIdAllocator();
		acceptedReceipts = 0;
		rejectedReceipts = 0;
		rejectedVanillaPackets = 0;
	}
}
