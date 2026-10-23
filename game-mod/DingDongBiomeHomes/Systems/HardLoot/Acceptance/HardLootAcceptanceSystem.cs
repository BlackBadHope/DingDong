using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace DingDongBiomeHomes.Systems.HardLoot.Acceptance;

public sealed class HardLootAcceptanceSystem : ModSystem
{
	private bool ran;
	private static bool Enabled => Main.dedServ &&
		Environment.GetEnvironmentVariable("DINGDONG_X10_LOOT_ACCEPTANCE") == "1";

	public override void Load()
	{
		HardLootBagBaseline.Reset();
		Arm("Load");
	}
	public override void Unload() => HardLootBagBaseline.Reset();
	public override void OnWorldLoad()
	{
		ran = false;
		Arm("OnWorldLoad");
	}
	public override void OnWorldUnload() => ran = false;
	// LoadWorld also reaches this hook after dedicated autogen -> save -> reload.
	// Empty dedicated servers need not advance PostUpdateWorld.
	public override void PostWorldLoad() => RunOnce("PostWorldLoad");
	public override void PostUpdateWorld() => RunOnce("PostUpdateWorld");

	private void Arm(string stage)
	{
		if (Enabled) Mod.Logger.Info($"DINGDONG_X10_LOOT_RUNTIME|verdict=ARMED|stage={stage}");
	}

	private void RunOnce(string stage)
	{
		if (ran || !Enabled) return;
		ran = true;
		Mod.Logger.Info($"DINGDONG_X10_LOOT_RUNTIME|verdict=START|stage={stage}");
		Player previous = Main.player[0];
		UnifiedRandom previousRandom = Main.rand;
		try
		{
			if (Main.netMode != NetmodeID.Server)
				throw new InvalidOperationException("Loot acceptance requires server net mode.");
			string path = (Main.ActiveWorldFileData?.Path ?? "").Replace('\\', '/');
			if (!path.Contains("/runtime-harness/runs/", StringComparison.OrdinalIgnoreCase) &&
				!path.Contains("/evidence/mp-runtime-", StringComparison.OrdinalIgnoreCase))
				throw new InvalidOperationException("Loot acceptance requires a disposable world.");
			if (Main.player.Any(player => player.active))
				throw new InvalidOperationException("Loot acceptance requires an empty server.");
			if (!Main.masterMode || !Main.getGoodWorld)
				throw new InvalidOperationException("Loot acceptance requires Master ForTheWorthy.");
			HardLootRuntimeGuard.EnforceBeforeNpcLoot();
			HardLootRuntimeGuard.AssertCurrent();
			Player actor = new() { whoAmI = 0, active = true, statLife = 100,
				position = new Vector2(Main.spawnTileX * 16, Main.spawnTileY * 16 - 48) };
			Main.player[0] = actor;
			Main.rand = new UnifiedRandom(271828);
			List<Exception> failures = [];
			int bags = 0, guarantees = 0, npcChecks = 0;
			string odds = "";
			try { (bags, guarantees) = HardLootBagAcceptance.Run(actor); }
			catch (Exception error) { failures.Add(error); }
			try { npcChecks = HardLootNpcAcceptance.Run(actor); }
			catch (Exception error) { failures.Add(error); }
			try { odds = HardLootOddsAcceptance.Run(actor); }
			catch (Exception error) { failures.Add(error); }
			if (failures.Count > 0) throw new AggregateException(failures);
			Mod.Logger.Info($"DINGDONG_X10_LOOT_RUNTIME|verdict=PASS|bags={bags}" +
				$"|guaranteedChecks={guarantees}|npcChecks={npcChecks}|{odds}|actualItemSpawn=True" +
				"|path=Player.OpenBossBag+ItemDropResolver|clientCombat=False" +
				"|guaranteeOracle=pre-normalization-snapshot+5-known-bags|fullInventoryRightClick=False");
		}
		catch (Exception error)
		{
			Mod.Logger.Error("DINGDONG_X10_LOOT_RUNTIME|verdict=FAIL|reason=" +
				error.Message.Replace('|', '/').Replace('\n', ' '), error);
		}
		finally { Main.player[0] = previous; Main.rand = previousRandom; }
	}
}
