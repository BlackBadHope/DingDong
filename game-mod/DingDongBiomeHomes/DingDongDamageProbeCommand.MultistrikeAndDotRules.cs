using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
/// <summary>
/// Non-mutating dedicated-server contract probe for the deterministic damage
/// core, persistent affinity metadata and multiplayer authority protocol.
/// </summary>
public sealed partial class DingDongDamageProbeCommand
{
	private static DamageSourceProfile VerifyMultistrikeAndDotRules(List<string> failures, DamageRuleEngine engine, DamageTargetProfile slime, DamageEvaluationTrace multi1, DamageEvaluationTrace multi2, DamageEvaluationTrace multi3)
	{
		Expect(failures, "true-multistrike-first", multi1.AcceptedIndependentEvent);
		Expect(failures, "true-multistrike-second", multi2.AcceptedIndependentEvent);
		ExpectMultiplier(failures, "true-multistrike-third-no-burst", multi3, 0.95d);
		DamageHitLedger expiryLedger = new();
		Evaluate(engine, DamageProfileRegistry.Bullet, slime, expiryLedger, 400, 21, 1);
		Evaluate(engine, DamageProfileRegistry.Bullet, slime, expiryLedger, 403, 21, 2);
		DamageEvaluationTrace expiredThird = Evaluate(engine, DamageProfileRegistry.Bullet, slime, expiryLedger, 404, 21, 3);
		ExpectMultiplier(failures, "rapid-window-removed", expiredThird, 0.95d);
		DamageSourceProfile damageOverTime = DamageSourceProfile.Single("dingdong:source/probe_dot", DamageTags.DamageOverTime, DamageTags.Poison, DamageTags.Sustained, DamageTags.UnspecifiedAcquisition);
		DamageEvaluationTrace damageOverTimeTrace = EvaluateOne(engine, damageOverTime, slime, 41);
		ExpectMultiplier(failures, "dot-slime-neutral-multiplier", damageOverTimeTrace, 1.00d);
		Expect(failures, "dot-bypasses-rapid-window-as-accepted", damageOverTimeTrace.AcceptedIndependentEvent);
		Expect(failures, "dot-bypasses-rapid-window-sequence-empty", damageOverTimeTrace.IndependentHitsInSequence == 0);
		DamageSourceProfile mixed = new("dingdong:source/probe_mixed", new WeightedDamageAxis("dingdong:axis/delivery", new[] { new WeightedDamageTag(DamageTags.KineticContact, 2d), new WeightedDamageTag(DamageTags.Bullet, 2d) }), WeightedDamageAxis.Single("dingdong:axis/element", DamageTags.Neutral), WeightedDamageAxis.Single("dingdong:axis/behavior", DamageTags.Single), WeightedDamageAxis.Single("dingdong:axis/acquisition", DamageTags.UnknownAcquisition));
		return mixed;
	}
}
