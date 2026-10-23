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
	internal static string RunProbe(Mod mod, string worldFile)
	{
		List<string> failures = new();
		DamageRuleEngine engine = new();
		DamageTargetProfile slime = DamageProfileRegistry.VanillaSlime;
		Expect(failures, "slime-stateful-burst-removed", slime.RapidHitRule == null);
		Expect(failures, "slime-minimum-balanced", Nearly(slime.MinimumMultiplier, 0.80d));
		Expect(failures, "slime-maximum-balanced", Nearly(slime.MaximumMultiplier, 1.30d));
		ServerAttackInstanceIdAllocator allocator = new();
		ulong firstIssuedLineage = allocator.Issue();
		ulong secondIssuedLineage = allocator.Issue();
		DamageEvaluationTrace rapid3, duplicate2, duplicate3;
		VerifyBaseMultipliersAndHitLineages(failures, engine, slime, firstIssuedLineage, secondIssuedLineage, out rapid3, out duplicate2, out duplicate3);
		Expect(failures, "duplicated-volley-second-does-not-drive-balance", duplicate2.AcceptedIndependentEvent);
		Expect(failures, "duplicated-volley-third-does-not-drive-balance", duplicate3.AcceptedIndependentEvent);
		ExpectMultiplier(failures, "duplicated-volley-no-burst", duplicate3, 0.95d);
		DamageHitLedger multistrikeLedger = new();
		DamageEvaluationTrace multi1 = Evaluate(engine, DamageProfileRegistry.Bullet, slime, multistrikeLedger, 300, 15, 1, true);
		DamageEvaluationTrace multi2 = Evaluate(engine, DamageProfileRegistry.Bullet, slime, multistrikeLedger, 301, 15, 2, true);
		DamageEvaluationTrace multi3 = Evaluate(engine, DamageProfileRegistry.Bullet, slime, multistrikeLedger, 302, 15, 3, true);
		DamageSourceProfile mixed = VerifyMultistrikeAndDotRules(failures, engine, slime, multi1, multi2, multi3);
		DamageEvaluationTrace mixedTrace = EvaluateOne(engine, mixed, slime, 50);
		ExpectMultiplier(failures, "weighted-geometric-mean", mixedTrace, Math.Sqrt(0.85d * 0.95d));
		Expect(failures, "normalized-delivery-axis", Nearly(mixed.Delivery.Tags.Sum(tag => tag.Weight), 1d));
		Expect(failures, "slime-staff-provenance", HasOnlyTag(DamageProfileRegistry.SlimeStaffCreatureDrop.Acquisition, DamageTags.CreatureDrop));
		Expect(failures, "finch-staff-provenance", HasOnlyTag(DamageProfileRegistry.FinchStaffWorldgen.Acquisition, DamageTags.Worldgen));
		ulong playerZeroAttack = DamageCombatPlayer.Compose(0, 77);
		DamageUiAuditResult uiAudit;
		DamageGuideAuditResult guideAudit;
		DamageDifficultySnapshot difficulty;
		string summary;
		ExecuteTwoPlayerProtocolScenario(mod, worldFile, failures, engine, slime, playerZeroAttack, out uiAudit, out guideAudit, out difficulty, out summary);
		mod.Logger.Info(uiAudit.ToMarker());
		mod.Logger.Info(guideAudit.ToMarker());
		mod.Logger.Info(difficulty.ToMarker());
		mod.Logger.Info($"DINGDONG_DAMAGE_TRACE|{rapid3.ToStableString()}");
		return summary;
	}
}
