using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes;

public sealed partial class NpcResidenceWorldSystem
{
	internal static void RememberVanillaHome(NPC npc, string npcKey)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient || npc is null || !npc.active || npc.homeless)
			return;
		ResidentEscortState state = GetState(npcKey);
		Point home = new(npc.homeTileX, npc.homeTileY);
		if (state.Stage != ResidentEscortStage.Arrived || state.DestinationHome == home ||
			!ResidentHousingDoctrineHooks.CanUseVanillaHousing(state.Stage, state.SpawnAnchor, home))
			return;
		// moveRoom can reject the requested room. Remember the actual home only
		// after vanilla has accepted it, so the AI and saved escort agree.
		state.DestinationHome = home;
		Touch(state);
		if (ResidenceFlagTileEntity.TryGet(state.FlagPosition, out ResidenceFlagTileEntity flag) &&
			flag.OwnerId == state.OwnerId && flag.NpcKey == npcKey)
		{
			flag.HomePoint = home;
			SyncFlag(flag);
		}
		npc.netUpdate = true;
		BroadcastSnapshot();
	}
}
