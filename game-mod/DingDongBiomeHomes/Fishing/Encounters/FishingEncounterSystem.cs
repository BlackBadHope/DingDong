using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Fishing.Encounters;

// No weather, invasion, blood-moon or foreign-mod event flags are changed.
// Event state is ephemeral and belongs to the server; joining clients receive WorldData.
public sealed class FishingEncounterSystem : ModSystem
{
	private const byte ProtocolVersion = 1;
	private static readonly int[] Active = new int[FishingEncounterPolicy.HabitatCount];
	private static readonly int[] Remaining = new int[FishingEncounterPolicy.HabitatCount];
	private static readonly int[] Cooldown = new int[FishingEncounterPolicy.HabitatCount];
	private static readonly int[] MissingFishing = new int[FishingEncounterPolicy.HabitatCount];
	private int scanTicks;
	public static IReadOnlyList<FishingEventSpec> Definitions => FishingEventCatalog.Definitions;
	public static string GetEventId(FishingHabitat habitat)
	{
		int h = (int)habitat;
		if (!FishingEncounterPolicy.IsHabitatIndex(h) || Remaining[h] <= 0) return "";
		int i = Active[h];
		return i >= 0 && i < Definitions.Count && Definitions[i].Habitat == habitat ? Definitions[i].Id : "";
	}

	public override void OnWorldLoad() => Reset();
	public override void OnWorldUnload() => Reset();
	private void Reset()
	{
		Array.Fill(Active, -1);
		Array.Clear(Remaining);
		Array.Clear(Cooldown);
		Array.Clear(MissingFishing);
		scanTicks = 0;
	}

	public override void PostUpdateWorld()
	{
		bool authority = FishingEncounterPolicy.IsAuthority(Main.netMode);
		bool changed = false;
		for (int h = 0; h < Remaining.Length; h++)
		{
			if (authority && Cooldown[h] > 0) Cooldown[h]--;
			if (Remaining[h] <= 0) continue;
			if (--Remaining[h] == 0 && authority)
			{
				StopEvent(h);
				changed = true;
			}
		}
		if (!authority) return;
		if (++scanTicks >= FishingEncounterPolicy.ScanInterval)
		{
			scanTicks = 0;
			changed |= ScanFishingPlayers();
		}
		if (changed && Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.WorldData);
	}

	private static bool ScanFishingPlayers()
	{
		bool changed = false;
		bool[] observed = new bool[FishingEncounterPolicy.HabitatCount];
		FishingConditions[] best = new FishingConditions[FishingEncounterPolicy.HabitatCount];
		Projectile[] bobbers = new Projectile[FishingEncounterPolicy.HabitatCount];
		Player[] fishers = new Player[FishingEncounterPolicy.HabitatCount];
		for (int p = 0; p < Main.maxProjectiles; p++)
		{
			Projectile bobber = Main.projectile[p];
			if (!TryObserveBobber(bobber, out Player player, out FishingConditions conditions)) continue;
			int h = (int)conditions.Habitat;
			if (!FishingEncounterPolicy.IsHabitatIndex(h)) continue;
			bool eligible = EligibleObservation(h, conditions);
			bool previousEligible = observed[h] && EligibleObservation(h, best[h]);
			if (observed[h] && (previousEligible && !eligible || previousEligible == eligible && best[h].FishingPower >= conditions.FishingPower)) continue;
			observed[h] = true;
			best[h] = conditions;
			bobbers[h] = bobber;
			fishers[h] = player;
		}
		for (int h = 0; h < Remaining.Length; h++)
		{
			if (Remaining[h] > 0)
			{
				if (!observed[h] || !FishingEncounterPolicy.CanContinue(Definitions[Active[h]], best[h]))
				{
					MissingFishing[h] += FishingEncounterPolicy.ScanInterval;
					if (MissingFishing[h] >= FishingEncounterPolicy.FishingGraceTicks) { StopEvent(h); changed = true; }
					continue;
				}
				MissingFishing[h] = 0;
				// One spawn attempt per habitat per scan; multibobber mods cannot multiply this rate.
				if (Main.rand.Next(5) == 0) TrySpawnEventEnemy(bobbers[h], fishers[h], best[h]);
				continue;
			}
			if (!observed[h] || Cooldown[h] > 0) continue;
			for (int i = 0; i < Definitions.Count; i++)
			{
				FishingEventSpec spec = Definitions[i];
				if (!spec.CanStart(best[h]) || Main.rand.Next(24) != 0) continue;
				Active[h] = i;
				Remaining[h] = spec.DurationTicks;
				MissingFishing[h] = 0;
				changed = true;
				Announce(spec, true);
				break;
			}
		}
		return changed;
	}
	private static bool EligibleObservation(int habitat, in FishingConditions conditions)
	{
		if (Remaining[habitat] > 0) return FishingEncounterPolicy.CanContinue(Definitions[Active[habitat]], conditions);
		foreach (FishingEventSpec spec in Definitions) if (spec.CanStart(conditions)) return true;
		return false;
	}

	private static bool TryObserveBobber(Projectile bobber, out Player player, out FishingConditions conditions)
	{
		player = null;
		conditions = default;
		if (bobber == null || !bobber.active || !bobber.bobber || !bobber.wet || bobber.ai[0] != 0
			|| bobber.owner < 0 || bobber.owner >= Main.maxPlayers) return false;
		player = Main.player[bobber.owner];
		if (player == null || !player.active || player.dead) return false;
		FishingRigSpec rig = bobber.GetGlobalProjectile<FishingBobber>().Rig(bobber);
		float distance = FishingEncounterPolicy.BobberDistanceLimit(rig.TargetDepthTiles);
		if (Vector2.DistanceSquared(player.Center, bobber.Center) > distance * distance) return false;
		int x = (int)(bobber.Center.X / 16f), y = (int)(bobber.Center.Y / 16f);
		if (!IsOpenWater(x, y)) return false;
		int power = Math.Max(0, player.GetFishingConditions().FinalFishingLevel);
		conditions = FishingEnvironment.AtPosition(x, y, power);
		return true;
	}

	internal static bool IsOpenWater(int x, int y)
	{
		if (x < 2 || y < 2 || x >= Main.maxTilesX - 2 || y >= Main.maxTilesY - 2) return false;
		Tile tile = Main.tile[x, y];
		return FishingEncounterPolicy.IsValidWaterTile(x, y, Main.maxTilesX, Main.maxTilesY,
			tile.LiquidAmount, tile.LiquidType, tile.HasTile && Main.tileSolid[tile.TileType]);
	}

	private static void TrySpawnEventEnemy(Projectile bobber, Player player, in FishingConditions c)
	{
		int type = GetCatchEnemyType(c);
		if (type <= 0) return;
		CountEnemies(type, bobber.Center, out int sameType, out int nearby, out int world);
		if (!FishingEncounterPolicy.MaySpawn(Main.netMode, bobber.wet, player.active, player.dead,
			GetEventId(c.Habitat).Length > 0, sameType, nearby, world)) return;
		int x0 = (int)(bobber.Center.X / 16f), y0 = (int)(bobber.Center.Y / 16f);
		for (int i = 0; i < 8; i++)
		{
			int x = x0 + Main.rand.Next(-22, 23), y = y0 + Main.rand.Next(3, 15);
			if (!IsOpenWater(x, y) || !IsOpenWater(x, y - 1) || !IsOpenWater(x - 1, y)
				|| !IsOpenWater(x + 1, y)) continue;
			Vector2 position = new(x * 16f, y * 16f);
			if (Vector2.DistanceSquared(player.Center, position) < 144f * 144f
				|| Collision.SolidCollision(position - new Vector2(24), 48, 48)) continue;
			FishingConditions here = FishingEnvironment.AtPosition(x, y, c.FishingPower);
			if (here.Habitat != c.Habitat) continue;
			NPC.NewNPC(bobber.GetSource_FromAI(), x * 16, y * 16, type, Target: player.whoAmI);
			return;
		}
	}

	private static void CountEnemies(int type, Vector2 at, out int sameType, out int nearby, out int world)
	{
		sameType = nearby = world = 0;
		for (int n = 0; n < Main.maxNPCs; n++)
		{
			NPC npc = Main.npc[n];
			if (!npc.active || npc.ModNPC is not FishingEncounterNpc) continue;
			world++;
			if (npc.type == type) sameType++;
			if (Vector2.DistanceSquared(npc.Center, at) < 1400f * 1400f) nearby++;
		}
	}

	// A type selection only. Calling this on the fishing owner's CatchFish hook uses the
	// existing Terraria npcSpawn route; it never allocates an NPC or sends a request.
	public static int GetCatchEnemyType(FishingConditions c)
	{
		if (!String.Equals(c.EventId, GetEventId(c.Habitat), StringComparison.Ordinal)) return 0;
		int eventIndex = FishingEventCatalog.IndexOf(c.EventId);
		if (eventIndex < 0 || !Definitions[eventIndex].CanStart(c)) return 0;
		FishingEnemySpec enemy = FishingEnemyCatalog.Definitions[eventIndex];
		if (enemy.RequiredProgression > c.Progression) return 0;
		int type = FishingEnemyCatalog.TypeAt(eventIndex), sameType = 0, world = 0;
		for (int i = 0; i < Main.maxNPCs; i++)
		{
			NPC npc = Main.npc[i];
			if (!npc.active || npc.ModNPC is not FishingEncounterNpc) continue;
			world++;
			if (npc.type == type) sameType++;
		}
		return sameType < FishingEncounterPolicy.MaximumEnemiesOfType
			&& world < FishingEncounterPolicy.MaximumWorldEnemies ? type : 0;
	}

	internal static float SpawnChanceFor(int enemyIndex, NPCSpawnInfo info)
	{
		if (!FishingEncounterPolicy.IsAuthority(Main.netMode) || !info.Water
			|| enemyIndex < 0 || enemyIndex >= FishingEnemyCatalog.Definitions.Count
			|| !IsOpenWater(info.SpawnTileX, info.SpawnTileY)) return 0;
		FishingConditions c = FishingEnvironment.AtPosition(info.SpawnTileX, info.SpawnTileY,
			Math.Max(0, info.Player.GetFishingConditions().FinalFishingLevel));
		if (c.EventId != FishingEnemyCatalog.Definitions[enemyIndex].EventId
			|| GetCatchEnemyType(c) != FishingEnemyCatalog.TypeAt(enemyIndex)) return 0;
		CountEnemies(FishingEnemyCatalog.TypeAt(enemyIndex), info.Player.Center,
			out int sameType, out int nearby, out int world);
		return FishingEncounterPolicy.MaySpawn(Main.netMode, true, info.Player.active, info.Player.dead,
			true, sameType, nearby, world) ? 0.12f : 0;
	}

	private static void StopEvent(int habitat)
	{
		int index = Active[habitat];
		if (index >= 0 && index < Definitions.Count) Announce(Definitions[index], false);
		Active[habitat] = -1;
		Remaining[habitat] = 0;
		MissingFishing[habitat] = 0;
		Cooldown[habitat] = FishingEncounterPolicy.CooldownTicks;
	}
	private static void Announce(in FishingEventSpec spec, bool started)
	{
		bool ru = Language.ActiveCulture.CultureInfo.TwoLetterISOLanguageName is "ru" or "uk";
		string name = ru ? spec.NameRu : spec.NameEn;
		string text = started ? (ru ? $"Рыбалка: {name}. Вода оживает!" : $"Fishing: {name}. The water stirs!")
			: (ru ? $"Рыбалка: {name} стихает." : $"Fishing: {name} subsides.");
		if (Main.netMode == NetmodeID.Server) ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(text), Color.LightSeaGreen);
		else Main.NewText(text, Color.LightSeaGreen);
	}

	public override void NetSend(BinaryWriter writer)
	{
		writer.Write(ProtocolVersion);
		writer.Write((byte)FishingEncounterPolicy.HabitatCount);
		for (int h = 0; h < Active.Length; h++)
		{
			writer.Write((byte)(Remaining[h] > 0 ? Active[h] : 255));
			writer.Write(Math.Clamp(Remaining[h], 0, FishingEncounterPolicy.MaximumDuration));
		}
	}
	public override void NetReceive(BinaryReader reader)
	{
		byte version = reader.ReadByte(), count = reader.ReadByte();
		if (version != ProtocolVersion || count != FishingEncounterPolicy.HabitatCount)
			throw new IOException("Unsupported DingDong fishing event state");
		for (int h = 0; h < Active.Length; h++)
		{
			int index = reader.ReadByte(), ticks = reader.ReadInt32();
			Remaining[h] = FishingEncounterPolicy.SanitizeRemaining(index, ticks, (FishingHabitat)h, Definitions);
			Active[h] = Remaining[h] > 0 ? index : -1;
		}
	}
}
