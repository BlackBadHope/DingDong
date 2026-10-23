using System;
using System.Collections.Generic;
using System.Linq;
using DingDongBiomeHomes;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.AiArsenal;
internal static partial class AiArsenalAudit
{
	private static void RunValidDamageClass(bool exerciseDamage, List<string> failures, ref int damageClassesPassed, ref int classificationsPassed, ref int damageCasesPassed, DamageRuleEngine engine, DamageHitLedger ledger, NPC target, DamageTargetClassification targetProfile, int caseIndex, AiArsenalItem modItem, Item item)
	{
		if (ValidDamageClass(item, modItem.Category))
			damageClassesPassed++;
		else
			failures.Add($"damage-class:{modItem.Name}");
		DamageSourceClassification classification = DamageRuntimeClassifier.Classify(item);
		string delivery = classification.Profile.Delivery.Tags[0].Id;
		string element = classification.Profile.Element.Tags[0].Id;
		string expectedDelivery = ExpectedDelivery(item, modItem.Category);
		string expectedElement = ExpectedElements.GetValueOrDefault(modItem.Name, DamageTags.Neutral);
		if (classification.Coverage == DamageCoverageKind.Adapter && string.Equals(delivery, expectedDelivery, StringComparison.Ordinal) && string.Equals(element, expectedElement, StringComparison.Ordinal))
		{
			classificationsPassed++;
		}
		else
		{
			failures.Add($"classification:{modItem.Name}:{delivery}:{element}:{classification.Coverage}");
		}

		if (exerciseDamage)
		{
			DamageEvaluationTrace trace = engine.EvaluateAcceptedHit(classification.Profile, targetProfile.Profile, new DamageHitObservation(caseIndex, 77, target.whoAmI, (ulong)caseIndex, 1, false), ledger, targetProfile.SegmentRole, targetProfile.PhaseRole);
			double finalDamage = item.damage * trace.FinalMultiplier;
			if (double.IsFinite(finalDamage) && finalDamage > 0d && trace.FinalMultiplier > 0d)
				damageCasesPassed++;
			else
				failures.Add($"damage:{modItem.Name}");
		}
	}

	private static bool ValidDamageClass(Item item, AiArsenalCategory category) => category switch
	{
		AiArsenalCategory.Melee => item.DamageType.CountsAsClass(DamageClass.Melee),
		AiArsenalCategory.Ranged => item.DamageType.CountsAsClass(DamageClass.Ranged),
		AiArsenalCategory.Magic => item.DamageType.CountsAsClass(DamageClass.Magic) && item.mana > 0,
		AiArsenalCategory.Summon => item.DamageType.CountsAsClass(DamageClass.Summon),
		_ => false
	};
}
