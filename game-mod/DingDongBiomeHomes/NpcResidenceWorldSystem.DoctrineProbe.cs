using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Utilities;
using DingDongBiomeHomes.Systems.HardLoot;
using DingDongBiomeHomes.Systems.HardLoot.Acceptance;
using System.Reflection;

namespace DingDongBiomeHomes;

// A bounded, in-memory dedicated probe. It exercises production entry points,
// not sockets, client rendering or actual pathfinding. The runner forbids saves.
public sealed partial class NpcResidenceWorldSystem
{
	internal static void RunDoctrineProbe(Mod mod)
	{
		var system = ModContent.GetInstance<NpcResidenceWorldSystem>();
		TagCompound original = new(); system.SaveWorldData(original);
		Player oldA = Main.player[0], oldB = Main.player[1];
		var ledger = ModContent.GetInstance<NpcQuestWorldLedger>();
		TagCompound originalLedger = new(); ledger.SaveWorldData(originalLedger);
		bool oldHard = Main.hardMode, oldDay = Main.dayTime;
		int oldMode = Main.netMode;
		float oldJumpSpeed = Player.jumpSpeed;
		int oldJumpHeight = Player.jumpHeight;
		UnifiedRandom oldRandom = Main.rand;
		var config = ModContent.GetInstance<DingDongServerConfig>();
		bool oldEscort = config.EnableNpcQuestsAndEscort, oldNight = config.EnableNightPressure, oldJump = config.EnableFiveTileBaseJump;
		int checks = 0;
		void Check(bool ok, string id) { if (!ok) throw new InvalidOperationException(id); checks++; mod.Logger.Info("DINGDONG_DOCTRINE_CASE|PASS|" + id); }
		try
		{
			if (oldA.active || oldB.active) throw new InvalidOperationException("Doctrine probe needs an empty dedicated server.");
			config.EnableNpcQuestsAndEscort = true; config.EnableNightPressure = true;
			config.EnableFiveTileBaseJump = true;
			system.ClearWorld(); Main.hardMode = false;
			Player a = Main.player[0] = new Player { whoAmI = 0, active = true };
			Player b = Main.player[1] = new Player { whoAmI = 1, active = true };
			a.GetModPlayer<NpcQuestPlayer>().SetNetworkIdentity("00112233445566778899aabbccddeeff");
			b.GetModPlayer<NpcQuestPlayer>().SetNetworkIdentity("ffeeddccbbaa99887766554433221100");
			string key = "Terraria/Guide";
			NpcTownManifest.TryGet(key, out NpcTownIdentity identity);
			NPC npc = new(); npc.SetDefaults(NPCID.Guide); npc.active = true;
			ResidentEscortState state = GetState(key);
			npc.Center = state.OriginHome.ToWorldCoordinates(); a.Center = npc.Center; b.Center = npc.Center;
			Vector2 start = npc.position;
			a.SpawnX = (int)(npc.Center.X / 16); a.SpawnY = (int)(npc.Center.Y / 16);
			TryBeginEscort(a, npc, key);
			Check(state.Stage == ResidentEscortStage.Anchored, "ESC-01 insufficient trust rejected");
			var progress = a.GetModPlayer<NpcQuestPlayer>().Get(key);
			progress.Completed = ResidentTrustPolicy.RequiredQuests(identity);
			progress.Active = true; TryBeginEscort(a, npc, key);
			Check(state.Stage == ResidentEscortStage.Anchored, "ESC-02 active quest blocks invitation");
			progress.Active = false;
			Main.netMode = NetmodeID.MultiplayerClient; TryBeginEscort(a, npc, key); Main.netMode = oldMode;
			Check(state.Stage == ResidentEscortStage.Anchored, "ESC-03 client cannot start escort");
			TryBeginEscort(a, npc, key);
			Check(state.Stage == ResidentEscortStage.Following && npc.position == start && state.SpawnAnchor == new Point(Main.spawnTileX, Main.spawnTileY), "ESC-04 explicit start no teleport world-spawn not bed");
			b.GetModPlayer<NpcQuestPlayer>().Get(key).Completed = 100;
			TryBeginEscort(b, npc, key);
			Check(state.OwnerId == a.GetModPlayer<NpcQuestPlayer>().PersistentId, "ESC-05 second owner cannot steal");
			Check(!CreatureTeleporterSystem.IsEligible(npc), "ESC-06 teleporter cannot skip walk");
			NPC merchant = new(); merchant.SetDefaults(NPCID.Merchant); merchant.active = true; merchant.Center = a.Center;
			a.GetModPlayer<NpcQuestPlayer>().Get("Terraria/Merchant").Completed = 100;
			TryBeginEscort(a, merchant, "Terraria/Merchant");
			Check(GetState("Terraria/Merchant").Stage == ResidentEscortStage.Anchored, "ESC-18 one escort per owner");
			Point remembered = state.SpawnAnchor;
			a.SpawnX += 500; a.SpawnY += 100;
			Check(state.SpawnAnchor == remembered, "ESC-07 changing bed cannot move anchor");
			npc.Center = a.Center = remembered.ToWorldCoordinates() + new Vector2(640, 0);
			Check(!TryCompleteEscort(npc, key, a), "ESC-08 outside spawn cannot unlock housing");
			npc.Center = remembered.ToWorldCoordinates();
			Check(!TryCompleteEscort(npc, key, a), "ESC-09 NPC alone cannot unlock housing");
			a.Center = npc.Center;
			Check(TryCompleteEscort(npc, key, a) && state.Stage == ResidentEscortStage.AwaitingHome, "ESC-10 both at spawn unlock selection only");
			MarkBefriended(key, b.GetModPlayer<NpcQuestPlayer>().PersistentId, b.whoAmI);
			Check(state.Stage == ResidentEscortStage.AwaitingHome && state.OwnerId == a.GetModPlayer<NpcQuestPlayer>().PersistentId, "ESC-11 legacy trust cannot steal awaiting resident");
			HandlePlayerDisconnected(0); HandlePlayerConnected(b);
			Check(state.OwnerPlayerIndex == -1, "ESC-12 wrong identity cannot reconnect escort");
			HandlePlayerConnected(a);
			Check(state.OwnerPlayerIndex == 0, "ESC-13 same identity reconnects escort");
			Check(WithinSettlementRadius(new Point16(2000, 1000), new Point(1000, 1000)) && !WithinSettlementRadius(new Point16(2001, 1000), new Point(1000, 1000)), "ESC-14 inclusive 1000 tile boundary");
			state.Stage = ResidentEscortStage.Following; state.FlagPosition = new Point16(500, 500); state.DestinationHome = new Point(501, 501);
			Check(!IsRoomVacant(new Rectangle(495, 495, 20, 20), NPCID.None, "another"), "ESC-15 pending room reserved against another NPC");
			TagCompound saved = new(); system.SaveWorldData(saved); system.LoadWorldData(saved);
			state = GetState(key);
			Check(state.OwnerId == a.GetModPlayer<NpcQuestPlayer>().PersistentId && state.SpawnAnchor == remembered && state.DestinationHome == new Point(501, 501), "ESC-16 save restore owner anchor destination");
			Check(NetworkContractPass(out _), "ESC-17 state wire roundtrip truncated envelope rejected");
			Check(ResidentTrustPolicy.RequiredQuests(3, HomeBiome.Forest, false) == 3 && ResidentTrustPolicy.RequiredQuests(3, HomeBiome.Underworld, true) == 14 && ResidentTrustPolicy.Percent(int.MaxValue, 3) == 100, "TRUST-01 tiers hardmode overflow");
			Check(!ResidentHousingDoctrineHooks.CanUseVanillaHousing(ResidentEscortStage.Anchored, remembered, remembered) &&
				!ResidentHousingDoctrineHooks.CanUseVanillaHousing(ResidentEscortStage.AwaitingHome, remembered, remembered) &&
				ResidentHousingDoctrineHooks.CanUseVanillaHousing(ResidentEscortStage.Arrived, remembered, new Point(remembered.X + 1000, remembered.Y)) &&
				!ResidentHousingDoctrineHooks.CanUseVanillaHousing(ResidentEscortStage.Arrived, remembered, new Point(remembered.X + 1001, remembered.Y)),
				"HOUSING-01 hidden and blocked before arrival, 1000 tile boundary enforced after arrival");
			Check(Worldgen.ContinentalSectionGuardPolicy.RebaseAfterDiscontinuity(4000, 0, 0) && !Worldgen.ContinentalSectionGuardPolicy.RebaseAfterDiscontinuity(100, 0, 10) && !Worldgen.ContinentalSectionGuardPolicy.RebaseAfterDiscontinuity(500, 0, 500), "STREAM-01 teleport invalidates stale anchor ordinary travel preserved");
			Check(Worldgen.ContinentalSectionGuardPolicy.RequestAt(1) && !Worldgen.ContinentalSectionGuardPolicy.RequestAt(2) && Worldgen.ContinentalSectionGuardPolicy.RequestAt(120), "STREAM-02 immediate request bounded retries");
			var pressure = new NightPressureGlobalNPC();
			foreach (var (day, sky, surface, rateExpected, capExpected) in new[] { (true, false, true, 600, 10), (false, false, true, 200, 30), (false, true, false, 200, 30), (false, false, false, 400, 15) })
			{
				Main.dayTime = day; a.ZoneSkyHeight = sky; a.ZoneOverworldHeight = surface;
				int rate = 600, cap = 10; pressure.EditSpawnRate(a, ref rate, ref cap);
				Check(rate == rateExpected && cap == capExpected, $"NIGHT-01 day={day} sky={sky} surface={surface}");
			}
			NPC hostile = new() { friendly = false, damage = 20 };
			foreach (bool day in new[] { true, false })
			{
				Main.dayTime = day;
				Player.HurtModifiers damage = new() { SourceDamage = StatModifier.Default };
				pressure.ModifyHitPlayer(hostile, a, ref damage);
				Check(damage.SourceDamage.ApplyTo(100) == (day ? 100 : 150), "NIGHT-02 actual contact damage day=" + day);
			}
			Projectile shot = new();
			var serverShot = new NightPressureGlobalProjectile();
			serverShot.OnSpawn(shot, new EntitySource_Parent(hostile));
			using (MemoryStream wire = new())
			{
				using (BinaryWriter writer = new(wire, System.Text.Encoding.UTF8, true))
				{
					BitWriter bits = new(); serverShot.SendExtraAI(shot, bits, writer); bits.Flush(writer);
				}
				wire.Position = 0;
				using BinaryReader reader = new(wire, System.Text.Encoding.UTF8, true);
				var clientShot = new NightPressureGlobalProjectile();
				clientShot.ReceiveExtraAI(shot, new BitReader(reader), reader);
				Player.HurtModifiers damage = new() { SourceDamage = StatModifier.Default };
				clientShot.ModifyHitPlayer(shot, a, ref damage);
				Check(damage.SourceDamage.ApplyTo(100) == 150 && wire.Position == wire.Length, "NIGHT-03 projectile origin wire roundtrip");
			}
			var jump = typeof(Player).GetMethod("JumpMovement", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).CreateDelegate<Action<Player>>();
			float NativeApex(float speed)
			{
				Player sample = new() { whoAmI = 0, gravDir = 1f, releaseJump = true, controlJump = true, blockExtraJumps = true };
				Player.jumpSpeed = speed; Player.jumpHeight = 15;
				float ascent = 0;
				for (int tick = 0; tick < 120; tick++)
				{
					jump(sample); sample.velocity.Y += 0.4f;
					if (sample.velocity.Y >= 0) return ascent;
					ascent -= sample.velocity.Y;
				}
				throw new InvalidOperationException("Native jump did not reach apex.");
			}
			float vanillaApex = NativeApex(FiveTileJumpPlayer.VanillaBaseJumpSpeed);
			float reducedApex = NativeApex(FiveTileJumpPlayer.HalfHeightBaseJumpSpeed);
			Check(Math.Abs(reducedApex - vanillaApex * .5f) < .01f, $"JUMP-01 native JumpMovement dry unobstructed apex {vanillaApex}->{reducedApex} pixels");
			Player.jumpSpeed = FiveTileJumpPlayer.VanillaBaseJumpSpeed + 2f;
			a.GetModPlayer<FiveTileJumpPlayer>().PostUpdateRunSpeeds();
			Check(Math.Abs(Player.jumpSpeed - FiveTileJumpPlayer.HalfHeightBaseJumpSpeed - 2f) < .0001f, "JUMP-02 equipment contribution preserved");
			int[] expected = new int[20000]; var random = new UnifiedRandom(7142);
			for (int i = 0; i < expected.Length; i++) expected[i] = random.Next(10000);
			foreach (float luck in new[] { -100f, 0f, 100f })
			{
				a.luck = luck; Main.rand = new UnifiedRandom(7142); bool exact = true;
				NpcLootLuckScope.Run(() => { for (int i = 0; i < expected.Length; i++) exact &= a.RollLuck(10000) == expected[i]; });
				Check(exact && a.luck == luck && !NpcLootLuckScope.Active, "LOOT-01 rare 1/10000 exact RNG unchanged luck=" + luck);
			}
			try { NpcLootLuckScope.Run(() => NpcLootLuckScope.Run(() => throw new IOException("test"))); } catch (IOException) { }
			Check(!NpcLootLuckScope.Active, "LOOT-02 nested exception restores scope");
			a.luck = 100; Main.rand = new UnifiedRandom(7142); bool differs = false;
			for (int i = 0; i < 100; i++) differs |= a.RollLuck(10000) != expected[i];
			Check(differs, "LOOT-03 non-loot luck not disabled");
			string odds = null;
			NpcLootLuckScope.Run(() => odds = HardLootOddsAcceptance.Run(a));
			Check(odds is not null, "LOOT-04 actual drop emissions " + odds);
			foreach (var report in SettlementWorldState.Residences)
			{
				for (int i = 0; i < 16; i++)
					if (!SettlementResidenceReport.CanStandAtQuestPoint(report.QuestPoint(i >= 8, i % 8))) throw new InvalidOperationException("QUEST blocked target: " + report.NpcKey + " " + i);
				using MemoryStream wire = new();
				using (BinaryWriter writer = new(wire, System.Text.Encoding.UTF8, true)) report.NetSend(writer);
				wire.Position = 0;
				using BinaryReader reader = new(wire, System.Text.Encoding.UTF8, true);
				var received = SettlementResidenceReport.NetReceive(reader);
				Main.netMode = NetmodeID.MultiplayerClient;
				bool matches = Enumerable.Range(0, 16).All(i => report.QuestPoint(i >= 8, i % 8) == received.QuestPoint(i >= 8, i % 8));
				Main.netMode = oldMode;
				Check(matches && wire.Position == wire.Length, "QUEST-01 standing targets and server-client coordinates " + report.NpcKey);
				var defense = new ResidentQuestProgress();
				defense.Begin(new ResidentQuestDefinition(0, ResidentQuestKind.DefendOrigin, 0, ItemID.None, 5, 20), 0, 0);
				Check(NpcQuestSystem.QuestMapSearchArea(report.NpcKey, defense, report.Area.Center, 20) == NpcQuestSystem.DefenseArea(report, 20), "QUEST-02 map defense equals kill-credit area " + report.NpcKey);
			}
			progress.Begin(new ResidentQuestDefinition(7, ResidentQuestKind.SurveyOrigin, 7, ItemID.None, 1, 7), 0, 0);
			Check(NpcQuestSystem.TryGetMapTarget(key, progress, out Point entrance, out _, out _), "QUEST-03 entrance has actual map target");
			a.Center = entrance.ToWorldCoordinates() + new Vector2(320, 0);
			NpcQuestSystem.UpdateLocationProgress(a);
			Check(progress.Progress == 0, "QUEST-04 outside marked entrance does not complete");
			a.Center = entrance.ToWorldCoordinates(); NpcQuestSystem.UpdateLocationProgress(a);
			Check(progress.Progress == 1, "QUEST-05 standing at marked entrance completes actual quest");
			DingDongAuthoredStructureLoader.ProbeGolfGrass(mod);
		}
		finally
		{
			Main.netMode = oldMode; Main.player[0] = oldA; Main.player[1] = oldB;
			Main.hardMode = oldHard; Main.dayTime = oldDay; Main.rand = oldRandom;
			Player.jumpSpeed = oldJumpSpeed; Player.jumpHeight = oldJumpHeight;
			config.EnableNpcQuestsAndEscort = oldEscort; config.EnableNightPressure = oldNight; config.EnableFiveTileBaseJump = oldJump;
			system.LoadWorldData(original);
			ledger.LoadWorldData(originalLedger);
		}
		mod.Logger.Info($"DINGDONG_DOCTRINE_CONTRACTS|verdict=PASS|checks={checks}|stateRestored=True|worldSaved=False|socketMultiplayer=False|pathfindingProven=False");
	}
}
