using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

internal static class ResidentHousingDoctrineHooks
{
	private const string LockedKey = "Mods.DingDongBiomeHomes.ResidenceFlag.VanillaHousingLocked";
	private const string OutsideRadiusKey = "Mods.DingDongBiomeHomes.ResidenceFlag.VanillaHousingOutsideRadius";
	private static bool installed;

	internal static void Install()
	{
		if (installed)
			return;
		On_WorldGen.moveRoom += MoveRoom;
		if (!Main.dedServ)
			On_Main.DrawNPCHousesInUI += DrawNpcHousesInUi;
		installed = true;
	}

	internal static void Uninstall()
	{
		if (!installed)
			return;
		On_WorldGen.moveRoom -= MoveRoom;
		if (!Main.dedServ)
			On_Main.DrawNPCHousesInUI -= DrawNpcHousesInUi;
		installed = false;
	}

	internal static bool CanUseVanillaHousing(ResidentEscortStage stage, Point spawnAnchor, Point destination) =>
		stage == ResidentEscortStage.Arrived &&
		spawnAnchor.X >= 0 && spawnAnchor.Y >= 0 &&
		NpcResidenceWorldSystem.WithinSettlementRadius(new Point16(destination.X, destination.Y), spawnAnchor);

	private static void MoveRoom(On_WorldGen.orig_moveRoom orig, int x, int y, int npcIndex)
	{
		if (!ModContent.GetInstance<DingDongServerConfig>().EnableNpcQuestsAndEscort ||
			npcIndex < 0 || npcIndex >= Main.maxNPCs)
		{
			orig(x, y, npcIndex);
			return;
		}

		NPC npc = Main.npc[npcIndex];
		if (!npc.active || !NpcResidentRegistry.TryGet(npc, out string npcKey, out _))
		{
			orig(x, y, npcIndex);
			return;
		}

		ResidentEscortState state = NpcResidenceWorldSystem.GetState(npcKey);
		if (CanUseVanillaHousing(state.Stage, state.SpawnAnchor, new Point(x, y)))
		{
			orig(x, y, npcIndex);
			NpcResidenceWorldSystem.RememberVanillaHome(npc, npcKey);
			return;
		}

		if (Main.netMode != NetmodeID.Server)
		{
			string key = state.Stage == ResidentEscortStage.Arrived ? OutsideRadiusKey : LockedKey;
			Main.NewText(Language.GetTextValue(key, NpcResidenceWorldSystem.SettlementRadiusTiles), 255, 196, 74);
		}
	}

	private static void DrawNpcHousesInUi(On_Main.orig_DrawNPCHousesInUI orig, Main self)
	{
		if (!ModContent.GetInstance<DingDongServerConfig>().EnableNpcQuestsAndEscort)
		{
			orig(self);
			return;
		}

		List<NPC> hidden = new();
		try
		{
			Player player = Main.LocalPlayer;
			NpcQuestPlayer questPlayer = player?.GetModPlayer<NpcQuestPlayer>();
			for (int index = 0; index < Main.maxNPCs; index++)
			{
				NPC npc = Main.npc[index];
				if (!npc.active || !NpcResidentRegistry.TryGet(npc, out string npcKey, out NpcTownIdentity identity))
					continue;
				if (CanDisplayHousingBanner(questPlayer, npcKey, identity))
					continue;
				// Terraria 1.4.4 DrawNPCHousesInUI enumerates active NPCs and never
				// reads townNPC. Temporarily hiding activity is therefore the only
				// effective draw-only filter; the value is restored in finally.
				npc.active = false;
				hidden.Add(npc);
			}
			orig(self);
		}
		finally
		{
			foreach (NPC npc in hidden)
				npc.active = true;
		}
	}

	internal static bool CanDisplayHousingBanner(NpcQuestPlayer questPlayer, string npcKey, NpcTownIdentity identity) =>
		questPlayer is not null && questPlayer.IdentityConfirmed &&
		questPlayer.TryGetCurrent(npcKey, out ResidentQuestProgress progress) &&
		progress.Completed >= ResidentTrustPolicy.RequiredQuests(identity);
}
