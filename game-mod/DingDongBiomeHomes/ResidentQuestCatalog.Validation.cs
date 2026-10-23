using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.ID;

namespace DingDongBiomeHomes;
internal static partial class ResidentQuestCatalog
{
	internal static bool ContractPass(out string failure)
	{
		failure = string.Empty;
		const string samplePlayer = "00000000000000000000000000000001";
		foreach (NpcTownIdentity identity in NpcTownManifest.All)
		{
			ResidentQuestDefinition[] catalog = Enumerable.Range(0, PoolSize).Select(id => Resolve(identity, id)).ToArray();
			int invalidIndex = Array.FindIndex(catalog, definition => !definition.IsValid);
			if (invalidIndex >= 0)
			{
				ResidentQuestDefinition invalid = catalog[invalidIndex];
				failure = $"catalog-invalid:{identity.NpcKey}:id={invalid.Id}:kind={invalid.Kind}:target={invalid.TargetKey}:item={invalid.ItemType}:goal={invalid.Goal}";
				return false;
			}

			IGrouping<int, ResidentQuestDefinition> duplicateId = catalog.GroupBy(definition => definition.Id).FirstOrDefault(group => group.Count() > 1);
			if (duplicateId != null)
			{
				failure = $"catalog-duplicate-id:{identity.NpcKey}:id={duplicateId.Key}";
				return false;
			}

			IGrouping<string, ResidentQuestDefinition> duplicateObjective = catalog.GroupBy(ObjectiveSignature, StringComparer.Ordinal).FirstOrDefault(group => group.Count() > 1);
			if (duplicateObjective != null)
			{
				failure = $"catalog-duplicate-objective:{identity.NpcKey}:{duplicateObjective.Key}:ids={string.Join(',', duplicateObjective.Select(value => value.Id).Take(4))}";
				return false;
			}

			ResidentQuestKind? missingKind = null;
			foreach (ResidentQuestKind kind in Enum.GetValues<ResidentQuestKind>())
			{
				if (catalog.All(definition => definition.Kind != kind))
				{
					missingKind = kind;
					break;
				}
			}

			if (missingKind.HasValue)
			{
				failure = $"catalog-missing-kind:{identity.NpcKey}:{missingKind.Value}";
				return false;
			}

			int[] rotation = Enumerable.Range(0, 3).SelectMany(cycle => BuildOrder(samplePlayer, identity.NpcKey, cycle)).ToArray();
			for (int cycle = 0; cycle < 3; cycle++)
			{
				if (rotation.Skip(cycle * PoolSize).Take(PoolSize).Distinct().Count() != PoolSize)
				{
					failure = $"rotation:{identity.NpcKey}:cycle={cycle}";
					return false;
				}
			}

			if (rotation.Zip(rotation.Skip(1), (left, right) => left == right).Any(equal => equal))
			{
				failure = $"repeat:{identity.NpcKey}";
				return false;
			}

			ResidentQuestDefinition[] guided = Enumerable.Range(0, GuidedKindBag.Length * 3).Select(completed => SelectAvailable(samplePlayer, identity, completed)).ToArray();
			if (guided.Take(3).Select(value => value.Kind).SequenceEqual(new[] { ResidentQuestKind.SurveyOrigin, ResidentQuestKind.DeliverSupplies, ResidentQuestKind.DefendOrigin }) == false)
			{
				failure = $"guided-onboarding:{identity.NpcKey}";
				return false;
			}

			int lastProgression = -1;
			for (int index = 3; index < guided.Length; index++)
			{
				if (guided[index].Kind is ResidentQuestKind.BossHunt or ResidentQuestKind.ModProgression)
				{
					if (lastProgression >= 0 && index - lastProgression > GuidedKindBag.Length - 1)
					{
						failure = $"guided-progression-gap:{identity.NpcKey}:{index - lastProgression}";
						return false;
					}

					lastProgression = index;
				}
			}
		}

		return true;
	}
}
