using System.Collections.Generic;
using DingDongBiomeHomes.Items.AccessoryEnchanting;
using DingDongBiomeHomes.Items.AiArsenal;
using DingDongBiomeHomes.Items.MobRelics;
using DingDongBiomeHomes.Items.SynergyArmor;

namespace DingDongBiomeHomes;

public sealed partial class AllModEndToEndProbeSystem
{
	private void VerifyPersistedWorld(string worldFile, AiArsenalAuditResult arsenal, MobRelicAuditResult mobRelics, SynergyArmorAuditResult synergyArmor, AccessoryEnchantingAuditResult accessoryEnchanting)
	{
		string persistence = DingDongQuestLifecycleProbeCommand.VerifyPersisted(Mod, worldFile);
		bool passed = setup.Passed && arsenal.Passed && mobRelics.Passed && synergyArmor.Passed && accessoryEnchanting.Passed && persistence.Contains("|verdict=PASS|");
		Mod.Logger.Info($"DINGDONG_ALLMOD_E2E_RELOAD|verdict={(passed ? "PASS" : "FAIL")}|enabled={setup.EnabledCount}/66|loaded={setup.LoadedCount}/{setup.ServerExpectedCount}|dedicatedExcluded={setup.DedicatedExcludedCount}|exactModSet={setup.ExactModSet}|aiArsenal={(arsenal.Passed ? "PASS" : "FAIL")}|mobRelics={(mobRelics.Passed ? "PASS" : "FAIL")}|synergyArmor={(synergyArmor.Passed ? "PASS" : "FAIL")}|accessoryEnchanting={(accessoryEnchanting.Passed ? "PASS" : "FAIL")}|questPersistence={PassFail(persistence)}|world={OneLine(worldFile)}|userSavesTouched=False");
	}

	private void LogPerModResults(QuestHookSnapshot hooks)
	{
		foreach (ModSnapshot mod in setup.Mods)
		{
			if (!mod.ServerApplicable)
			{
				Mod.Logger.Info($"DINGDONG_ALLMOD_E2E_MOD|mod={OneLine(mod.Name)}|enabled=True|loaded=False|applicability=DedicatedExcluded|package=PASS|damageMatrix=N/A|projectileHook=N/A|questKillHook=N/A|questKillCases=0/0|questTemplates=0|modCall=N/A|syntheticProvider=False|verdict=PASS");
				continue;
			}
			int eligible = hooks.EligibleByMod.GetValueOrDefault(mod.Name);
			int passed = hooks.PassedByMod.GetValueOrDefault(mod.Name);
			string questKill = eligible == 0 ? "N/A" : eligible == passed ? "PASS" : "FAIL";
			bool modPassed = mod.CallPassed && mod.DamagePassed && questKill != "FAIL";
			Mod.Logger.Info($"DINGDONG_ALLMOD_E2E_MOD|mod={OneLine(mod.Name)}|enabled=True|loaded=True|applicability=Dedicated|package=PASS|items={mod.Items}|projectiles={mod.Projectiles}|hostileNpcs={mod.HostileNpcs}|bosses={mod.Bosses}|townNpcs={mod.TownNpcs}|buffs={mod.Buffs}|biomes={mod.Biomes}|damageMatrix={(mod.DamagePassed ? "PASS" : "FAIL")}|projectileHook={mod.ProjectileHook}|questKillHook={questKill}|questKillCases={passed}/{eligible}|questTemplates={mod.QuestTemplates}|modCall={(mod.CallPassed ? "PASS" : "FAIL")}|syntheticProvider=True|verdict={(modPassed ? "PASS" : "FAIL")}");
		}
	}

	private void LogAggregateResult(string worldFile, AiArsenalAuditResult arsenal, MobRelicAuditResult mobRelics, SynergyArmorAuditResult synergyArmor, AccessoryEnchantingAuditResult accessoryEnchanting, string lifecycle, MultiplayerAbProbeResult multiplayer, QuestHookSnapshot hooks, bool passed, int modsPassed, int serverModsPassed)
	{
		Mod.Logger.Info($"DINGDONG_ALLMOD_E2E|verdict={(passed ? "PASS" : "FAIL")}|enabled={setup.EnabledCount}/66|loaded={setup.LoadedCount}/{setup.ServerExpectedCount}|dedicatedExcluded={setup.DedicatedExcludedCount}|exactModSet={setup.ExactModSet}|aiArsenal={(arsenal.Passed ? "PASS" : "FAIL")}|mobRelics={(mobRelics.Passed ? "PASS" : "FAIL")}|synergyArmor={(synergyArmor.Passed ? "PASS" : "FAIL")}|accessoryEnchanting={(accessoryEnchanting.Passed ? "PASS" : "FAIL")}|multiplayerAB={(multiplayer.Passed ? "PASS" : "FAIL")}|damageProtocol={setup.DamageProtocol}|modCalls={setup.CallPassed}/{setup.ServerExpectedCount}|projectileHooks={setup.ProjectileHooksPassed}/{setup.ProjectileHooksEligible}|questLifecycle={PassFail(lifecycle)}|questKillHooks={hooks.PassedCases}/{hooks.EligibleCases}|serverModsPassed={serverModsPassed}/{setup.ServerExpectedCount}|modsCovered={modsPassed}/{setup.EnabledCount}|world={OneLine(worldFile)}|saveReloadPending=True|userSavesTouched=False");
	}
}
