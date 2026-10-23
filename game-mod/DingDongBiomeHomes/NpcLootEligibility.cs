using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes;

internal static class NpcLootEligibility
{
	private static readonly HashSet<string> ExplicitServiceActors = new(StringComparer.Ordinal)
	{
		"CalamityMod/SuperDummyNPC",
		"Fargowiltas/SuperDummy",
		"ImproveGame/DummyNPC",
		"Terraria/TargetDummy",
		"ThoriumMod/AggroDummy",
		"ThoriumMod/BossDummy",
		"ThoriumMod/HealingDummy",
		"ThoriumMod/IllusionistDecoy"
	};

	internal static IReadOnlySet<string> ExplicitlyExcludedContent => ExplicitServiceActors;

	internal static bool IsExplicitServiceActor(string contentKey) =>
		!string.IsNullOrWhiteSpace(contentKey) && ExplicitServiceActors.Contains(contentKey);

	internal static bool IsEligible(NPC npc)
	{
		if (npc == null || npc.type <= NPCID.None || npc.friendly || npc.townNPC || npc.lifeMax <= 5)
			return false;
		return !IsExplicitServiceActor(QuestContentRegistry.ContentKey(npc));
	}
}
