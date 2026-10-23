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
	private static void PruneApprovals(long serverTick)
	{
		PendingApprovals.Prune(serverTick);
	}

	private static void PruneClientCommits(long clientTick)
	{
		List<int> expired = new();
		foreach ((int targetId, ClientPendingCommit pending)in ClientPendingCommits)
		{
			if (pending.ExpiresAtTick < clientTick)
				expired.Add(targetId);
		}

		foreach (int targetId in expired)
			ClientPendingCommits.Remove(targetId);
	}
}
