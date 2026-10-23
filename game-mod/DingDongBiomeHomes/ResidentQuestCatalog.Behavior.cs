using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.ID;

namespace DingDongBiomeHomes;
internal static partial class ResidentQuestCatalog
{
	internal static int LegacyQuestId(int completed) => completed switch
	{
		0 => 0,
		1 => SurveyCount + PatrolCount,
		2 => SurveyCount + PatrolCount + SupplyCount,
		_ => -1
	};
	private static ResidentQuestKind PreferredKind(string persistentId, string npcKey, int completed)
	{
		if (completed == 0)
			return ResidentQuestKind.SurveyOrigin;
		if (completed == 1)
			return ResidentQuestKind.DeliverSupplies;
		if (completed == 2)
			return ResidentQuestKind.DefendOrigin;
		int adjusted = completed - 3;
		int bagCycle = adjusted / GuidedKindBag.Length;
		int bagPosition = adjusted % GuidedKindBag.Length;
		ResidentQuestKind[] bag = (ResidentQuestKind[])GuidedKindBag.Clone();
		StableRandom random = new(StableHash($"quest-3.0-kind|{persistentId}|{npcKey}|{bagCycle}"));
		int blockLength = bag.Length / 2;
		for (int block = 0; block < 2; block++)
		{
			int start = block * blockLength;
			for (int index = start + blockLength - 1; index > start; index--)
			{
				int other = start + random.Next(index - start + 1);
				(bag[index], bag[other]) = (bag[other], bag[index]);
			}
		}

		return bag[bagPosition];
	}

	private static (int ItemType, int BaseCount, int Step) ArchetypeSupply(HomeArchetype archetype) => archetype switch
	{
		HomeArchetype.Scholar => (ItemID.Book, 2, 1),
		HomeArchetype.Trader => (ItemID.SilverCoin, 15, 5),
		HomeArchetype.Healer => (ItemID.LesserHealingPotion, 8, 1),
		HomeArchetype.Engineer => (ItemID.Wire, 15, 5),
		HomeArchetype.Mystic => (ItemID.FallenStar, 8, 1),
		HomeArchetype.Angler => (ItemID.Worm, 4, 2),
		HomeArchetype.Warrior => (ItemID.Gel, 35, 5),
		HomeArchetype.Artist => (ItemID.ClayBlock, 20, 5),
		HomeArchetype.Chef => (ItemID.Mushroom, 5, 2),
		HomeArchetype.Entertainer => (ItemID.Confetti, 30, 10),
		HomeArchetype.Naturalist => (ItemID.Acorn, 5, 2),
		HomeArchetype.Royal => (ItemID.Silk, 3, 1),
		_ => (ItemID.Torch, 20, 5)};
	private static string ObjectiveSignature(ResidentQuestDefinition definition) => definition.Kind switch
	{
		ResidentQuestKind.SurveyOrigin => $"survey:{definition.Variant}",
		ResidentQuestKind.PatrolOrigin => $"patrol:{definition.Variant}:{definition.Goal}",
		ResidentQuestKind.DeliverSupplies => $"supply:{definition.ItemType}:{definition.Goal}",
		ResidentQuestKind.DefendOrigin => $"defense:{definition.Goal}:{definition.RadiusTiles}",
		ResidentQuestKind.BossHunt => $"boss:{definition.TargetKey}",
		ResidentQuestKind.BiomePurge => $"biome:{definition.TargetKey}:{definition.Goal}",
		ResidentQuestKind.BannerCollection => $"banner:{definition.TargetKey}:{definition.ItemType}",
		ResidentQuestKind.ModProgression => $"progression:{definition.TargetKey}",
		_ => $"unknown:{definition.Id}"};
	private static uint StableHash(string value)
	{
		unchecked
		{
			uint hash = 2166136261u;
			foreach (char character in value)
			{
				hash ^= character;
				hash *= 16777619u;
			}

			return hash == 0 ? 0x9E3779B9u : hash;
		}
	}
}
