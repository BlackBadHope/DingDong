using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.MobRelics;

internal static partial class MobRelicAudit
{
	internal static MobRelicAuditResult Run(Mod mod)
	{
		MobRelicAuditContext context = new(mod);
		AuditLoadedSources(context);
		foreach (NPC mob in context.LoadedMobs)
			AuditMobSource(context, mob);
		MobRelicVisualPlayer visualProbe = new();
		foreach (GeneratedMobRelicItem relic in context.RegisteredItems)
			AuditRelicItem(context, visualProbe, relic);
		AuditResonanceProjectile(context);
		if (MobRelicGameplayProbe.Run(out string gameplayFailure))
			context.RuntimeGameplayPassed = 1;
		else
			Failure(context, $"runtime-gameplay:{gameplayFailure}");
		return CompleteAudit(context);
	}

	internal static MobRelicPlayer EnsureProbeRelicPlayer(Player player)
	{
		try
		{
			return player.GetModPlayer<MobRelicPlayer>();
		}
		catch (IndexOutOfRangeException)
		{
			// PostWorldLoad precedes normal dedicated-server player initialization.
		}

		MethodInfo setup = typeof(PlayerLoader).GetMethod("SetupPlayer", BindingFlags.Static | BindingFlags.NonPublic) ?? throw new MissingMethodException(typeof(PlayerLoader).FullName, "SetupPlayer");
		try
		{
			setup.Invoke(null, new object[] { player });
		}
		catch (TargetInvocationException exception) when (exception.InnerException != null)
		{
			throw new InvalidOperationException("Mob relic probe-player initialization failed.", exception.InnerException);
		}
		return player.GetModPlayer<MobRelicPlayer>();
	}

	private static bool SameIdentity(Item expected, Item actual) => actual?.ModItem is GeneratedMobRelicItem actualRelic && expected.ModItem is GeneratedMobRelicItem expectedRelic && actual.type == expected.type && actual.stack == expected.stack && actualRelic.Tier == expectedRelic.Tier && actualRelic.Source.ContentKey == expectedRelic.Source.ContentKey && actualRelic.Name == expectedRelic.Name;
	private static bool Localized(string value) => !string.IsNullOrWhiteSpace(value) && !value.StartsWith("Mods.DingDongBiomeHomes.Items.", StringComparison.Ordinal);

	private static void Failure(MobRelicAuditContext context, string value)
	{
		if (context.Failures.Count < 24)
			context.Failures.Add(value);
	}
}
