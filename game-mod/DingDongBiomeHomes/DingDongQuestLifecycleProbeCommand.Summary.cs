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
	private static string BuildLifecycleProbeSummary(Mod mod, string worldFile, List<string> failures, Player player, NPC npc, int accepted, int completed, int gated, int rewards, int residentsAccepted, int residentsCompleted, int residentRewards, bool milestoneBonus, bool journalAccepted, bool journalReady, bool persistence)
	{
		Expect(failures, "save-load-readback", persistence);
		QuestJournalSnapshot completedSnapshot = QuestJournalContent.Build(player);
		bool journalLocalized = Localized(completedSnapshot.Title) && Localized(completedSnapshot.Subtitle) && Localized(completedSnapshot.Summary);
		Expect(failures, "journal-accepted", journalAccepted);
		Expect(failures, "journal-ready", journalReady);
		Expect(failures, "journal-localized", journalLocalized);
		player.active = false;
		npc.active = false;
		string verdict = failures.Count == 0 ? "PASS" : "FAIL";
		string summary = $"DINGDONG_QUEST_LIFECYCLE_PROBE|verdict={verdict}|world={OneLine(worldFile)}|accepted={accepted}/5|completed={completed}/5|prematureTurnInRejected={gated == 1}|rewards={rewards}/5|residentsAccepted={residentsAccepted}/{NpcTownManifest.AuthoredCount}|residentsCompleted={residentsCompleted}/{NpcTownManifest.AuthoredCount}|residentRewards={residentRewards}/{NpcTownManifest.AuthoredCount}|rewardDelivery=world-drop|milestoneBonus={milestoneBonus}|journalAccepted={journalAccepted}|journalReady={journalReady}|journalLocalized={journalLocalized}|saveLoadReadback={persistence}|failures={OneLine(string.Join(',', failures))}|syntheticPlayer=True|syntheticResidences={SettlementWorldState.Residences.Count}|userSavesTouched=False";
		mod.Logger.Info(summary);
		return summary;
	}
}
