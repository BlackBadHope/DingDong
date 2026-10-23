using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DingDongBiomeHomes.Items.AccessoryEnchanting;
using DingDongBiomeHomes.Items.AiArsenal;
using DingDongBiomeHomes.Items.MobRelics;
using DingDongBiomeHomes.Items.SynergyArmor;
using Terraria;
using Terraria.IO;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;

public sealed partial class AllModEndToEndProbeSystem
{
	private void RunWorldPhase()
	{
		if (ranWorldPhase || !Enabled())
			return;
		string worldFile = Path.GetFileNameWithoutExtension(Main.ActiveWorldFileData?.Path ?? string.Empty);
		ranWorldPhase = true;
		string phase = Phase();
		try
		{
			if (!worldFile.StartsWith(WorldPrefix, StringComparison.Ordinal))
				throw new InvalidOperationException($"Unexpected all-mod E2E world '{worldFile}'.");
			if (Main.player.Any(player => player.active))
				throw new InvalidOperationException("All-mod E2E requires an empty dedicated server.");
			AiArsenalAuditResult arsenal = AiArsenalAudit.Run(Mod, requireRecipes: true, exerciseDamage: phase == "prepare");
			MobRelicAuditResult mobRelics = MobRelicAudit.Run(Mod);
			SynergyArmorAuditResult synergyArmor = SynergyArmorAudit.Run(Mod, requireRecipes: true, exerciseGameplay: phase == "prepare");
			AccessoryEnchantingAuditResult accessoryEnchanting = AccessoryEnchantingAudit.Run(Mod, exerciseGameplay: phase == "prepare");
			if (phase == "verify")
			{
				VerifyPersistedWorld(worldFile, arsenal, mobRelics, synergyArmor, accessoryEnchanting);
				return;
			}
			if (phase != "prepare")
				throw new InvalidOperationException($"Unsupported all-mod E2E phase '{phase}'.");

			// Lifecycle and A/B fixtures replace the registry with synthetic rooms.
			// Keep the real loaded residence geometry for the subsequent world save.
			SettlementWorldState settlements = new();
			NpcResidenceWorldSystem residences = new();
			TagCompound savedSettlements = new(), savedResidences = new();
			settlements.SaveWorldData(savedSettlements);
			residences.SaveWorldData(savedResidences);
			string lifecycle;
			MultiplayerAbProbeResult multiplayerAb;
			QuestHookSnapshot questHooks;
			try
			{
				lifecycle = DingDongQuestLifecycleProbeCommand.RunProbe(Mod, worldFile);
				multiplayerAb = DingDongMultiplayerAbProbe.Run(Mod);
				questHooks = RunQuestKillHooks();
			}
			finally
			{
				settlements.LoadWorldData(savedSettlements);
				residences.LoadWorldData(savedResidences);
			}
			bool passed = setup.Passed && arsenal.Passed && mobRelics.Passed && synergyArmor.Passed && accessoryEnchanting.Passed && lifecycle.Contains("|verdict=PASS|", StringComparison.Ordinal) && multiplayerAb.Passed && questHooks.Passed;
			LogPerModResults(questHooks);
			int modsPassed = setup.Mods.Count(mod => !mod.ServerApplicable || mod.CallPassed && mod.DamagePassed && questHooks.PassedByMod.GetValueOrDefault(mod.Name) == questHooks.EligibleByMod.GetValueOrDefault(mod.Name));
			int serverModsPassed = setup.Mods.Count(mod => mod.ServerApplicable && mod.CallPassed && mod.DamagePassed && questHooks.PassedByMod.GetValueOrDefault(mod.Name) == questHooks.EligibleByMod.GetValueOrDefault(mod.Name));
			LogAggregateResult(worldFile, arsenal, mobRelics, synergyArmor, accessoryEnchanting, lifecycle, multiplayerAb, questHooks, passed, modsPassed, serverModsPassed);
			if (!passed)
			{
				Mod.Logger.Error($"DINGDONG_ALLMOD_E2E_SAVED|verdict=FAIL|world={OneLine(worldFile)}|reason=aggregate-failed|userSavesTouched=False");
				return;
			}
			SaveProbeWorld(worldFile);
		}
		catch (Exception exception)
		{
			Main.player[0].active = false;
			Main.npc[0].active = false;
			Mod.Logger.Error($"DINGDONG_ALLMOD_E2E|verdict=FAIL|phase={OneLine(phase)}|exception={exception.GetType().Name}|message={OneLine(exception.Message)}|world={OneLine(worldFile)}|userSavesTouched=False", exception);
		}
	}

	private void SaveProbeWorld(string worldFile)
	{
		try
		{
			WorldFile.SaveWorld(useCloudSaving: false, resetTime: false);
			Mod.Logger.Info($"DINGDONG_ALLMOD_E2E_SAVED|verdict=PASS|world={OneLine(worldFile)}|productionSave=WorldFile.SaveWorld|saveReloadPending=True|userSavesTouched=False");
		}
		catch (Exception exception)
		{
			Mod.Logger.Error($"DINGDONG_ALLMOD_E2E_SAVED|verdict=FAIL|world={OneLine(worldFile)}|exception={exception.GetType().Name}|message={OneLine(exception.Message)}|userSavesTouched=False", exception);
			throw;
		}
	}
}
