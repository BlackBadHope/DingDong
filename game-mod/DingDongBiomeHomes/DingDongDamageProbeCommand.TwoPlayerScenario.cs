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
	private static void ExecuteTwoPlayerProtocolScenario(Mod mod, string worldFile, List<string> failures, DamageRuleEngine engine, DamageTargetProfile slime, ulong playerZeroAttack, out DamageUiAuditResult uiAudit, out DamageGuideAuditResult guideAudit, out DamageDifficultySnapshot difficulty, out string summary)
	{
		ulong playerOneAttack = DamageCombatPlayer.Compose(1, 77);
		Expect(failures, "player-zero-lineage-nonzero", playerZeroAttack != 0);
		Expect(failures, "player-one-lineage-distinct", playerOneAttack != playerZeroAttack);
		Expect(failures, "player-zero-lineage-owner", DamageCombatPlayer.BelongsToPlayer(playerZeroAttack, 0));
		Expect(failures, "player-zero-lineage-cross-owner-rejected", !DamageCombatPlayer.BelongsToPlayer(playerZeroAttack, 1));
		Expect(failures, "player-one-lineage-owner", DamageCombatPlayer.BelongsToPlayer(playerOneAttack, 1));
		DamageHitIntent intentZero = new(DamageSourceKind.Item, 11, 1309, -1, playerZeroAttack, 1, 700, 0.95f);
		DamageHitIntent intentOne = new(DamageSourceKind.Projectile, 11, 1, 903, playerOneAttack, 17, 701, 1.18f);
		DamageCoverageSnapshot coverage;
		DamageExhaustiveAuditResult exhaustive;
		RunProtocolCoverageChecks(failures, engine, slime, playerZeroAttack, playerOneAttack, intentZero, intentOne, out coverage, out exhaustive, out uiAudit, out guideAudit, out difficulty);
		int assertions = 118;
		string verdict = failures.Count == 0 ? "PASS" : "FAIL";
		summary = $"DINGDONG_DAMAGE_PROBE|verdict={verdict}|assertions={assertions - failures.Count}/{assertions}|failures={OneLine(string.Join(',', failures))}|world={worldFile}|worldMutated=False|liveCombatHook=True|twoPlayerReplica=True|serverAuthority=protocol-contract-proven|coverageUnknown={coverage.Unknown}";
		mod.Logger.Info(summary);
		mod.Logger.Info(coverage.ToMarker());
		mod.Logger.Info(exhaustive.ToMarker());
	}
}
