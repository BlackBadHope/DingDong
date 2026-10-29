using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;
public sealed partial class DingDongQuestLifecycleProbeCommand
{
	private static int SyntheticPlayerIndex => Main.player.Length - 1;
	private static int SyntheticNpcIndex => Main.maxNPCs - 1;

	private static void InitializeLifecycleProbe(out List<string> failures, out NpcQuestWorldLedger ledger, out Player player, out NPC npc, out NpcTownIdentity identity, out NpcQuestPlayer questPlayer, out int accepted, out int completed, out int gated, out int rewards, out bool milestoneBonus, out bool journalAccepted, out bool journalReady, out int residentsAccepted, out int residentsCompleted, out int residentRewards)
	{
		failures = new();
		ledger = new();
		NpcResidenceWorldSystem residenceSystem = new();
		SettlementWorldState settlementState = new();
		if (SyntheticPlayerIndex < Main.maxPlayers || SyntheticNpcIndex < 0)
			throw new InvalidOperationException("Dedicated-server test slots are unavailable.");

		player = Main.player[SyntheticPlayerIndex];
		npc = Main.npc[SyntheticNpcIndex];
		ledger.ClearWorld();
		residenceSystem.ClearWorld();
		settlementState.ClearWorld();
		BuildSyntheticResidenceRegistry(failures);
		if (!NpcTownManifest.TryGet(LifecycleResidentKey, out identity) || !NpcResidentRegistry.TryGetType(identity.NpcKey, out int npcType))
		{
			throw new InvalidOperationException("Guide resident identity is unavailable.");
		}

		foreach (Item item in player.inventory)
			item.TurnToAir();
		player.active = false;
		player.dead = false;
		player.whoAmI = SyntheticPlayerIndex;
		player.position = new Vector2(1600f, 1600f);
		EnsureProbeModPlayers(player);
		SetNpcDefaultsForEarlyProbe(npc, npcType, player);
		npc.whoAmI = SyntheticNpcIndex;
		npc.active = true;
		npc.position = player.position + new Vector2(32f, 0f);
		// Production interaction handling rejects inactive players. Activate the
		// synthetic fixture only after NPC defaults have run: foreign GlobalNPC
		// hooks may inspect active-player vanilla arrays during this early phase.
		player.active = true;
		questPlayer = player.GetModPlayer<NpcQuestPlayer>();
		questPlayer.SetNetworkIdentity(ProbeIdentity);
		NpcQuestWorldLedger.BindPlayer(player, allowLocalImport: false);
		accepted = 0;
		completed = 0;
		gated = 0;
		rewards = 0;
		milestoneBonus = false;
		journalAccepted = false;
		journalReady = false;
		residentsAccepted = 0;
		residentsCompleted = 0;
		residentRewards = 0;
	}

	internal static void EnsureProbeModPlayers(Player player)
	{
		try
		{
			_ = player.GetModPlayer<NpcQuestPlayer>();
			return;
		}
		catch (IndexOutOfRangeException)
		{
		// PostWorldLoad precedes the normal dedicated-server player setup.
		}

		MethodInfo setupPlayer = typeof(PlayerLoader).GetMethod("SetupPlayer", BindingFlags.Static | BindingFlags.NonPublic) ?? throw new MissingMethodException(typeof(PlayerLoader).FullName, "SetupPlayer");
		try
		{
			setupPlayer.Invoke(null, new object[] { player });
		}
		catch (TargetInvocationException exception)when (exception.InnerException != null)
		{
			throw new InvalidOperationException("tModLoader probe-player initialization failed.", exception.InnerException);
		}

		_ = player.GetModPlayer<NpcQuestPlayer>();
	}

	internal static void InteractWithSyntheticResident(Player player, NPC npc)
	{
		// These reserved slots are outside the server's connection pool. Reassert
		// activity because the empty-server update loop can deactivate test entities.
		player.active = true;
		npc.active = true;
		NpcQuestSystem.HandleInteraction(player, npc.whoAmI, requestEscort: false);
	}

	internal static void SetNpcDefaultsForEarlyProbe(NPC npc, int npcType, params Player[] syntheticPlayers)
	{
		bool[] active = syntheticPlayers.Select(candidate => candidate.active).ToArray();
		try
		{
			foreach (Player candidate in syntheticPlayers)
				candidate.active = false;
			npc.SetDefaults(npcType);
		}
		finally
		{
			for (int index = 0; index < syntheticPlayers.Length; index++)
				syntheticPlayers[index].active = active[index];
		}
	}
}
