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
	private static void VerifyTwoPlayerReplicaParity(List<string> failures, DamageRuleEngine engine, DamageTargetProfile slime, ulong playerZeroAttack, ulong playerOneAttack, DamageHitIntent intentZero, DamageStrikePayload payloadZero, DamageHitCommit commitZero, out DamageHitLedger playerZeroClientLedger, out DamageHitLedger playerOneClientLedger, out DamageHitLedger replicatedServerLedger, out DamageEvaluationTrace p0Client3, out DamageEvaluationTrace p0Server3, out DamageEvaluationTrace p1Client1, out DamageEvaluationTrace p1Server1)
	{
		DamageEvaluationTrace p0Client1;
		VerifyApprovalTransactionAndInitializeLedgers(failures, engine, slime, playerZeroAttack, intentZero, payloadZero, commitZero, out playerZeroClientLedger, out playerOneClientLedger, out replicatedServerLedger, out p0Client1);
		DamageEvaluationTrace p0Server1 = Evaluate(engine, DamageProfileRegistry.Bullet, slime, replicatedServerLedger, 1000, playerZeroAttack, 1, attackerId: 0);
		DamageEvaluationTrace p0Client2 = Evaluate(engine, DamageProfileRegistry.Bullet, slime, playerZeroClientLedger, 1001, DamageCombatPlayer.Compose(0, 78), 1, attackerId: 0);
		DamageEvaluationTrace p0Server2 = Evaluate(engine, DamageProfileRegistry.Bullet, slime, replicatedServerLedger, 1001, DamageCombatPlayer.Compose(0, 78), 1, attackerId: 0);
		p0Client3 = Evaluate(engine, DamageProfileRegistry.Bullet, slime, playerZeroClientLedger, 1002, DamageCombatPlayer.Compose(0, 79), 1, attackerId: 0);
		p0Server3 = Evaluate(engine, DamageProfileRegistry.Bullet, slime, replicatedServerLedger, 1002, DamageCombatPlayer.Compose(0, 79), 1, attackerId: 0);
		p1Client1 = Evaluate(engine, DamageProfileRegistry.Bullet, slime, playerOneClientLedger, 1002, playerOneAttack, 1, attackerId: 1);
		p1Server1 = Evaluate(engine, DamageProfileRegistry.Bullet, slime, replicatedServerLedger, 1002, playerOneAttack, 1, attackerId: 1);
		Expect(failures, "player-zero-first-client-server-parity", SameMultiplier(p0Client1, p0Server1));
		Expect(failures, "player-zero-second-client-server-parity", SameMultiplier(p0Client2, p0Server2));
		Expect(failures, "player-zero-third-client-server-parity", SameMultiplier(p0Client3, p0Server3));
	}
}
