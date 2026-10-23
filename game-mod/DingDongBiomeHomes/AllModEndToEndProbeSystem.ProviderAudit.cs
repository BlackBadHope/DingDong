using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

public sealed partial class AllModEndToEndProbeSystem
{
	private void AuditProvider(SetupAuditContext context, string name, bool registerCalls, bool runDamage)
	{
		if (!context.ServerSet.Contains(name))
		{
			context.Mods.Add(ModSnapshot.DedicatedExcluded(name));
			return;
		}
		if (!ModLoader.TryGetMod(name, out Mod provider))
		{
			context.Mods.Add(ModSnapshot.ServerMissing(name));
			context.Failures.Add($"missing:{name}");
			return;
		}

		List<Item> items = ContentSamples.ItemsByType.Values.Where(item => item.ModItem?.Mod?.Name == name).ToList();
		List<Projectile> projectiles = ContentSamples.ProjectilesByType.Values.Where(projectile => projectile.ModProjectile?.Mod?.Name == name).ToList();
		List<NPC> npcs = ContentSamples.NpcsByNetId.Values.Where(npc => npc.ModNPC?.Mod?.Name == name).ToList();
		int hostileNpcs = npcs.Count(IsQuestKillEligible);
		int bosses = npcs.Count(npc => IsQuestKillEligible(npc) && npc.boss);
		int townNpcs = npcs.Count(npc => npc.townNPC);
		int buffs = ModContent.GetContent<ModBuff>().Count(buff => buff.Mod.Name == name);
		int biomes = ModContent.GetContent<ModBiome>().Count(biome => biome.Mod.Name == name);
		int templates = context.Templates.Count(template => TemplateProvider(template) == name);
		bool callPassed = !registerCalls || ExerciseRegistrationCall(name, provider);
		if (callPassed) context.CallsPassed++; else context.Failures.Add($"mod-call:{name}");
		string projectileHook = ExerciseProviderProjectile(context, name, projectiles, runDamage);
		context.Mods.Add(new ModSnapshot(name, true, true, items.Count, projectiles.Count, hostileNpcs, bosses, townNpcs, buffs, biomes, templates, context.DamagePassed && projectileHook != "FAIL", projectileHook, callPassed));
	}

	private bool ExerciseRegistrationCall(string name, Mod provider)
	{
		bool complete = false;
		const string key = "allmod-e2e";
		string targetKey = $"milestone:{name}/{key}";
		bool first = Mod.Call("RegisterQuestMilestone", name, key, $"{provider.DisplayName} E2E milestone", (Func<bool>)(() => complete)) is true;
		bool initiallyIncomplete = !QuestContentRegistry.IsExternalMilestoneComplete(targetKey);
		bool duplicateRejected = Mod.Call("RegisterQuestMilestone", name, key, "duplicate", (Func<bool>)(() => true)) is false;
		complete = true;
		bool completionObserved = QuestContentRegistry.IsExternalMilestoneComplete(targetKey);
		return first && initiallyIncomplete && duplicateRejected && completionObserved;
	}

	private static string ExerciseProviderProjectile(SetupAuditContext context, string name, List<Projectile> projectiles, bool runDamage)
	{
		Projectile representative = projectiles.FirstOrDefault(projectile => projectile.damage > 0);
		if (representative == null || !runDamage)
			return "N/A";
		context.ProjectileHooksEligible++;
		bool passed = RunProjectileHook(representative.type);
		if (passed) context.ProjectileHooksPassed++; else context.Failures.Add($"projectile-hook:{name}");
		return passed ? "PASS" : "FAIL";
	}
}
