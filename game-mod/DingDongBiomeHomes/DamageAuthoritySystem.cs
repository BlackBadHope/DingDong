using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
/// <summary>
/// Applies deterministic client prediction and validates the same decision on
/// the server before the vanilla DamageNPC packet is allowed to mutate life.
/// </summary>
public sealed partial class DamageAuthoritySystem : ModSystem
{
	private const long ApprovalLifetimeTicks = 120;
	private const long ClientCommitLifetimeTicks = 2;
	private const float MultiplierTolerance = 0.0005f;
	private const int MaximumPendingPlayers = 256;
	private static readonly DamageRuleEngine ClientEngine = new();
	private static readonly DamageRuleEngine ServerEngine = new();
	private static readonly DamageHitLedger ClientLedger = new();
	private static readonly DamageHitLedger ServerWorldLedger = new();
	private static readonly Dictionary<int, DamageHitLedger> ServerPlayerLedgers = new();
	private static ServerAttackInstanceIdAllocator serverAllocator = new();
	private static readonly DamageApprovalBook PendingApprovals = new(MaximumPendingPlayers);
	private static readonly Dictionary<int, ClientPendingCommit> ClientPendingCommits = new();
	private static readonly Dictionary<int, long> LatestClientTick = new();
	private static int acceptedReceipts;
	private static int rejectedReceipts;
	private static long rejectedVanillaPackets;
	public static int AcceptedReceipts => acceptedReceipts;
	public static int RejectedReceipts => rejectedReceipts;
	private static bool Enabled => ModContent.GetInstance<DingDongServerConfig>().EnableDamageSystem;
	internal static bool IsEnabled => Enabled;
	private static bool StrictValidation => ModContent.GetInstance<DingDongServerConfig>().StrictMultiplayerDamageValidation;
}
