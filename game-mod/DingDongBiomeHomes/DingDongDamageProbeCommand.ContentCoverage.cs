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
	private static DamageCoverageSnapshot VerifyApprovalConsumptionAndContentCoverage(List<string> failures, DamageRuleEngine engine, DamageTargetProfile slime, ulong playerZeroAttack, ulong playerOneAttack, DamageHitIntent intentZero, DamageStrikePayload payloadZero, DamageStrikePayload payloadOne, DamageHitCommit commitZero, DamageHitCommit commitOne, DamageApprovalBook approvalBook)
	{
		Expect(failures, "approval-commit-player-one", approvalBook.TryCommit(1, commitOne, 801) == DamageApprovalResult.Committed);
		Expect(failures, "approval-payload-mismatch-rejected", approvalBook.TryConsume(0, 11, payloadZero with { Damage = 43 }, 802, out _) == DamageApprovalResult.PayloadMismatch);
		VerifyTransactionFallbackAndClassifications(failures, engine, slime, playerZeroAttack, playerOneAttack, intentZero, payloadZero, payloadOne, commitZero, approvalBook);
		Expect(failures, "boss-loot-provenance", DamageAffinityGlobalItem.ResolveLootAcquisition(new NPC { boss = true }, DamageTags.UnspecifiedAcquisition) == DamageTags.BossDrop);
		Expect(failures, "creature-loot-provenance", DamageAffinityGlobalItem.ResolveLootAcquisition(new NPC(), DamageTags.UnspecifiedAcquisition) == DamageTags.CreatureDrop);
		Expect(failures, "boss-bag-provenance", DamageAffinityGlobalItem.ResolveOpenedItemAcquisition(ItemID.KingSlimeBossBag, DamageTags.UnspecifiedAcquisition) == DamageTags.BossDrop);
		Expect(failures, "fishing-crate-provenance", DamageAffinityGlobalItem.ResolveOpenedItemAcquisition(ItemID.WoodenCrate, DamageTags.UnspecifiedAcquisition) == DamageTags.Fishing);
		Expect(failures, "generic-container-does-not-claim-biome-chest", DamageAffinityGlobalItem.ResolveOpenedItemAcquisition(ItemID.GoodieBag, DamageTags.Event) == DamageTags.Event);
		DamageCoverageSnapshot coverage = DamageRuntimeClassifier.AuditLoadedContent();
		Expect(failures, "coverage-items-present", coverage.Items > 0);
		Expect(failures, "coverage-projectiles-present", coverage.Projectiles > 0);
		Expect(failures, "coverage-hostile-npcs-present", coverage.HostileNpcs > 0);
		Expect(failures, "coverage-unknown-zero", coverage.Unknown == 0);
		return coverage;
	}
}
