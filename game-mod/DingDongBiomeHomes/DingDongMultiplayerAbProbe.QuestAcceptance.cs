using System;
using System.Collections.Generic;
using System.Linq;
using DingDongBiomeHomes.Items.AccessoryEnchanting;
using DingDongBiomeHomes.Items.MobRelics;
using DingDongBiomeHomes.Items.SynergyArmor;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;
internal static partial class DingDongMultiplayerAbProbe
{
	private static int AcceptFirstPlayerQuestAndVerifyIsolation(Player playerA, NPC resident, NpcTownIdentity identity, List<string> failures, int passed, NpcQuestPlayer questA, NpcQuestPlayer questB, out ResidentQuestProgress aAccepted, out ResidentQuestProgress bAccepted, out bool[] beforeDuplicate)
	{
		NpcQuestNetwork.HandleQuestInteractionForProbe(0, resident.whoAmI, requestEscort: false);
		aAccepted = questA.Get(identity.NpcKey);
		ResidentQuestProgress bBaseline = questB.Get(identity.NpcKey);
		Check(failures, "quest:A-isolated-accept", aAccepted.Active && !bBaseline.Active, ref passed);
		bool[] beforePremature = CaptureActiveItems();
		NpcQuestNetwork.HandleQuestInteractionForProbe(0, resident.whoAmI, requestEscort: false);
		Check(failures, "quest:A-premature-gated", aAccepted.Active && NewWorldItems(beforePremature).Count == 0, ref passed);
		NpcQuestNetwork.HandleQuestInteractionForProbe(1, resident.whoAmI, requestEscort: false);
		bAccepted = questB.Get(identity.NpcKey);
		Check(failures, "quest:parallel-accept", aAccepted.Active && bAccepted.Active && !ReferenceEquals(aAccepted, bAccepted), ref passed);
		ResidentQuestProgress bBeforeACompletion = bAccepted.Clone();
		DingDongQuestLifecycleProbeCommand.PrepareObjective(playerA, aAccepted, aAccepted.Definition(identity));
		NpcQuestWorldLedger.Record(playerA, identity.NpcKey, aAccepted);
		bool[] beforeAReward = CaptureActiveItems();
		NpcQuestNetwork.HandleQuestInteractionForProbe(0, resident.whoAmI, requestEscort: false);
		List<Item> aReward = NewWorldItems(beforeAReward);
		Check(failures, "quest:A-complete", !aAccepted.Active && aAccepted.Completed == 1 && aReward.Count > 0, ref passed);
		Check(failures, "quest:B-unmodified", Equivalent(bAccepted, bBeforeACompletion), ref passed);
		beforeDuplicate = CaptureActiveItems();
		NpcQuestNetwork.HandleQuestInteractionForProbe(0, resident.whoAmI, requestEscort: false);
		return passed;
	}
}
