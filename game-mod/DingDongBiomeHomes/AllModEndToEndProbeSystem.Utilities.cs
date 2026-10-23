using System;
using System.Linq;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes;

public sealed partial class AllModEndToEndProbeSystem
{
	private static bool RunProjectileHook(int projectileType)
	{
		try
		{
			NPC target = new();
			target.SetDefaults(NPCID.BlueSlime);
			target.whoAmI = 0;
			Projectile projectile = new();
			projectile.SetDefaults(projectileType);
			projectile.owner = Main.maxPlayers;
			projectile.whoAmI = 0;
			projectile.identity = projectileType;
			projectile.active = true;
			NPC.HitModifiers modifiers = target.GetIncomingStrikeModifiers(projectile.DamageType, 1, false);
			DamageAuthoritySystem.ApplyProjectileHit(target, projectile, ref modifiers);
			float finalDamage = modifiers.FinalDamage.ApplyTo(Math.Max(1, projectile.damage));
			return float.IsFinite(finalDamage) && finalDamage > 0f;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsQuestKillEligible(NPC npc) => !npc.friendly && !npc.townNPC && npc.lifeMax > 5 && npc.damage > 0;

	private static string TemplateProvider(UniversalQuestTemplate template)
	{
		if (template.TargetKey.StartsWith("milestone:", StringComparison.Ordinal))
			return ProviderBeforeSlash(template.TargetKey["milestone:".Length..]);
		if (template.TargetKey.StartsWith("ModBiome:", StringComparison.Ordinal))
			return ProviderBeforeSlash(template.TargetKey["ModBiome:".Length..]);
		int separator = template.TargetKey.IndexOf('/');
		return separator >= 0 ? template.TargetKey[..separator] : template.Kind == ResidentQuestKind.ModProgression ? template.TargetKey : "Terraria";
	}

	private static string ProviderBeforeSlash(string value)
	{
		int slash = value.IndexOf('/');
		return slash >= 0 ? value[..slash] : value;
	}

	private static bool Enabled() => Main.dedServ && Environment.GetEnvironmentVariable(EnabledEnvironment) == "1";
	private static string Phase() => Environment.GetEnvironmentVariable(PhaseEnvironment)?.Trim().ToLowerInvariant() ?? string.Empty;
	private static string[] ExpectedMods() => ParseExpected(ExpectedModsEnvironment);
	private static string[] ExpectedServerMods() => ParseExpected(ExpectedServerModsEnvironment);
	private static string[] ParseExpected(string variable) => (Environment.GetEnvironmentVariable(variable) ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).OrderBy(name => name, StringComparer.Ordinal).ToArray();
	private static string PassFail(string marker) => marker.Contains("|verdict=PASS|", StringComparison.Ordinal) ? "PASS" : "FAIL";
	private static string OneLine(string value) => string.IsNullOrWhiteSpace(value) ? "none" : value.Replace('\r', ' ').Replace('\n', ' ').Replace('|', '/');
}
