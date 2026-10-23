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
	private static void VerifyBaseMultipliersAndHitLineages(List<string> failures, DamageRuleEngine engine, DamageTargetProfile slime, ulong firstIssuedLineage, ulong secondIssuedLineage, out DamageEvaluationTrace rapid3, out DamageEvaluationTrace duplicate2, out DamageEvaluationTrace duplicate3)
	{
		Expect(failures, "server-lineage-nonzero", firstIssuedLineage != 0);
		Expect(failures, "server-lineage-unique", secondIssuedLineage == firstIssuedLineage + 1);
		ExpectMultiplier(failures, "kinetic", EvaluateOne(engine, DamageProfileRegistry.KineticContact, slime, 10), 0.85d);
		ExpectMultiplier(failures, "arrow", EvaluateOne(engine, DamageProfileRegistry.Arrow, slime, 20), 0.90d);
		ExpectMultiplier(failures, "bullet", EvaluateOne(engine, DamageProfileRegistry.Bullet, slime, 30), 0.95d);
		DamageHitLedger rapidLedger = new();
		DamageEvaluationTrace rapid1 = Evaluate(engine, DamageProfileRegistry.Bullet, slime, rapidLedger, 100, 1, 1);
		DamageEvaluationTrace rapid2 = Evaluate(engine, DamageProfileRegistry.Bullet, slime, rapidLedger, 101, 2, 1);
		rapid3 = Evaluate(engine, DamageProfileRegistry.Bullet, slime, rapidLedger, 102, 3, 1);
		ExpectMultiplier(failures, "rapid-first-normalized", rapid1, 0.95d);
		ExpectMultiplier(failures, "rapid-second-normalized", rapid2, 0.95d);
		ExpectMultiplier(failures, "rapid-third-no-hidden-burst", rapid3, 0.95d);
		Expect(failures, "rapid-trace-has-no-hidden-condition", rapid3.ConditionalRule == "none");
		DamageHitLedger duplicateLedger = new();
		DamageEvaluationTrace duplicate1 = Evaluate(engine, DamageProfileRegistry.Bullet, slime, duplicateLedger, 200, 9, 1);
		duplicate2 = Evaluate(engine, DamageProfileRegistry.Bullet, slime, duplicateLedger, 201, 9, 2);
		duplicate3 = Evaluate(engine, DamageProfileRegistry.Bullet, slime, duplicateLedger, 202, 9, 3);
		Expect(failures, "duplicated-volley-first-accepted", duplicate1.AcceptedIndependentEvent);
	}
}
