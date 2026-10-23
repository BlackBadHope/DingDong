using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes;

public sealed partial class AllModEndToEndProbeSystem
{
	private static bool ExerciseQuestKill(NpcQuestGlobalNPC hook, NpcQuestPlayer questPlayer, Player player, NpcTownIdentity guide, SettlementResidenceReport residence, NPC sample, out string failure)
	{
		try
		{
			ResidentQuestProgress progress = questPlayer.Get(guide.NpcKey);
			progress.Begin(new ResidentQuestDefinition(900_000 + sample.type, ResidentQuestKind.DefendOrigin, guide.Variant, ItemID.None, 1, 0, string.Empty, sample.FullName, 100), 100, 0);
			NpcQuestWorldLedger.Record(player, guide.NpcKey, progress);
			NPC defeated = (NPC)sample.Clone();
			defeated.playerInteraction = new bool[Main.maxPlayers];
			defeated.whoAmI = 1;
			defeated.active = false;
			defeated.friendly = false;
			defeated.townNPC = false;
			defeated.lastInteraction = 0;
			defeated.Center = new Vector2(residence.Area.Center.X * 16f, residence.Area.Center.Y * 16f);
			if (QuestContentRegistry.ContentKey(defeated) != QuestContentRegistry.ContentKey(sample))
				throw new InvalidOperationException("cloned NPC lost its content identity");
			if (defeated.boss)
			{
				// Regression contract: several bosses, including Queen Slime, may
				// enter OnKill with zero current contact damage.
				defeated.damage = 0;
				defeated.playerInteraction[0] = true;
			}
			hook.OnKill(defeated);
			if (progress.Progress != 1)
				throw new InvalidOperationException($"progress={progress.Progress}");
			failure = string.Empty;
			return true;
		}
		catch (Exception exception)
		{
			failure = exception.GetType().Name;
			return false;
		}
	}
}
