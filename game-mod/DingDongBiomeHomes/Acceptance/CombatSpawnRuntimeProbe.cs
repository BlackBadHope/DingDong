using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Reflection;
using MonoMod.RuntimeDetour;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using DingDongBiomeHomes.Compatibility;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Acceptance;

// Explicit disposable-profile diagnostic. Never enabled by a release launcher.
public sealed class CombatSpawnRuntimeProbe : ModSystem
{
	public override void PostAddRecipes()
	{
		if (!Main.dedServ || Environment.GetEnvironmentVariable("DINGDONG_COMBAT_PROBE") != "1") return;
		int netMode = Main.netMode, myPlayer = Main.myPlayer, width = Main.maxTilesX, height = Main.maxTilesY;
		bool menu = Main.gameMenu;
		Player[] players = Main.player; NPC[] npcs = Main.npc; Projectile[] projectiles = Main.projectile;
		var observations = new List<object>();
		var traces = new List<ILHook>();
		try
		{
			Main.netMode = NetmodeID.SinglePlayer; Main.myPlayer = 0; Main.gameMenu = false;
			Main.maxTilesX = 4200; Main.maxTilesY = 1200;
			Main.player = Enumerable.Range(0, 256).Select(i => new Player { whoAmI = i }).ToArray();
			Main.npc = Enumerable.Range(0, Main.maxNPCs + 1).Select(i => new NPC { whoAmI = i }).ToArray();
			Main.projectile = Enumerable.Range(0, Main.maxProjectiles + 1).Select(i => new Projectile { whoAmI = i }).ToArray();
			Player player = Main.player[0]; player.active = true; player.statLife = player.statLifeMax = 500;
			player.Center = new Vector2(16000, 12000); player.selectedItem = 0;
			// BossChecklist initializes its entry table during the normal world-load phase.
			ModLoader.GetMod("BossChecklist").GetContent<ModSystem>().First(s => s.GetType().FullName == "BossChecklist.Systems.RecordSystem").OnWorldLoad();
			if (Environment.GetEnvironmentVariable("DINGDONG_COMBAT_TRACE") == "1")
				TraceDefaults(observations, traces);
			Sample("before", player, observations);
			ModItem summon = ModContent.Find<ModItem>("Fargowiltas/Abeemination2");
			player.inventory[0].SetDefaults(summon.Type); player.inventory[0].stack = 1;
			var source = new EntitySource_ItemUse_WithAmmo(player, player.inventory[0], 0);
			player.inventory[0].ModItem.Shoot(player, source, player.Center, Vector2.Zero, player.inventory[0].shoot, 0, 0);
			ObserveBoss("after-Shoot-before-Kill", observations);
			int spawnType = ModContent.Find<ModProjectile>("Fargowiltas/SpawnProj").Type;
			observations.Add(new { stage = "service-projectiles", projectiles = Main.projectile.Where(p => p.active).Select(p => new { p.type, p.whoAmI, p.owner, ai = p.ai.ToArray() }).ToArray(), expected = spawnType });
			foreach (Projectile projectile in Main.projectile.Where(p => p.active && p.type == spawnType).ToArray()) projectile.Kill();
			ObserveBoss("ordinary-summon", observations);
			Sample("after-ordinary-summon", player, observations);
			foreach (NPC npc in Main.npc) npc.active = false;
			ModItem swarm = ModContent.Find<ModItem>("Fargowiltas/OverloadBee");
			player.inventory[0].SetDefaults(swarm.Type); player.inventory[0].stack = 1;
			player.inventory[0].ModItem.UseItem(player);
			ObserveBoss("swarm-summon", observations);
			Sample("after-swarm-summon", player, observations);
			int moneyTroughChecks = FargoMoneyTroughCompatibility.RunRuntimeChecks();
			int nativeMoneyTroughChecks = CheckNativeMoneyTrough(player);
			int multiplierChecks = CheckMultiplier(player);
			int emptyNpcs = Main.npc.Count(n => n.active && n.type <= 0);
			bool clean = emptyNpcs == 0 && !observations.Any(o => o.GetType().GetProperty("error")?.GetValue(o) != null);
			Write(new { status = clean ? "PASS" : "FAIL", observations, moneyTroughChecks, nativeMoneyTroughChecks, multiplierChecks, emptyNpcs, limits = "Native loaded hooks; synthetic ordinary singleplayer state, dedicated executable; no full client item input or boss AI replay." });
		}
		catch (Exception error) { Write(new { status = "FAIL", observations, error = error.ToString() }); }
		finally
		{
			foreach (ILHook trace in traces) trace.Dispose();
			Main.netMode = netMode; Main.myPlayer = myPlayer; Main.gameMenu = menu;
			Main.maxTilesX = width; Main.maxTilesY = height;
			Main.player = players; Main.npc = npcs; Main.projectile = projectiles;
		}
	}
	private static int CheckNativeMoneyTrough(Player player)
	{
		Type configType = ModLoader.GetMod("Fargowiltas").Code.GetType("Fargowiltas.Common.Configs.FargoServerConfig");
		object config = configType.GetField("Instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
		FieldInfo field = configType.GetField("StalkerMoneyTrough"); object saved = field.GetValue(config);
		try
		{
			field.SetValue(config, true);
			Item item = new(); item.SetDefaults(ItemID.MoneyTrough);
			var source = player.GetSource_ItemUse(item);
			int old = Projectile.NewProjectile(source, player.Center, Vector2.Zero, ProjectileID.FlyingPiggyBank, 0, 0, player.whoAmI);
			int replacement = Projectile.NewProjectile(source, player.Center + new Vector2(100, 0), Vector2.Zero, ProjectileID.FlyingPiggyBank, 0, 0, player.whoAmI);
			Main.projectile[old].Kill();
			if (Main.projectile[old].active || !Main.projectile[replacement].active || Main.projectile[replacement].timeLeft <= 0) throw new InvalidOperationException("Native old pig Kill expired replacement");
			for (int tick = 0; tick < 3; tick++) Main.projectile[replacement].Update(replacement);
			if (!Main.projectile[replacement].active || Main.projectile[replacement].timeLeft <= 0) throw new InvalidOperationException("Native replacement pig AI expired");
			Main.projectile[replacement].Kill();
			if (Main.projectile[replacement].active) throw new InvalidOperationException("Native single pig Kill suppressed");
			return 3;
		}
		finally { field.SetValue(config, saved); }
	}
	private static int CheckMultiplier(Player player)
	{
		Type type = ModLoader.GetMod("danmaku").Code.GetType("danmaku.Proj100Config", true);
		object settings = typeof(ModContent).GetMethods().Single(m => m.Name == "GetInstance" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0).MakeGenericMethod(type).Invoke(null, null);
		FieldInfo count = type.GetField("AdditionalProjectiles"), enabled = type.GetField("EnableMod");
		object savedCount = count.GetValue(settings), savedEnabled = enabled.GetValue(settings);
		Projectile[] savedProjectiles = Main.projectile;
		int checks = 0;
		try
		{
			enabled.SetValue(settings, true);
			Item bow = new(); bow.SetDefaults(ItemID.WoodenBow);
			foreach (int extra in new[] { 0, 1, 2, 3 })
			{
				count.SetValue(settings, extra);
				Main.projectile = Enumerable.Range(0, Main.maxProjectiles + 1).Select(i => new Projectile { whoAmI = i }).ToArray();
				Projectile.NewProjectile(player.GetSource_ItemUse(bow), player.Center + new Vector2(200, 0), new Vector2(2, 0), ProjectileID.WoodenArrowFriendly, 10, 0, player.whoAmI, 17, 23, 31);
				Projectile[] shots = Main.projectile.Where(p => p.active).ToArray();
				if (shots.Length != extra + 1 || shots.Any(p => p.ai[0] != 17 || p.ai[1] != 23 || p.ai[2] != 31)) throw new InvalidOperationException("Danmaku native count/AI mismatch: " + extra);
				checks += 2;
			}
			count.SetValue(settings, 3);
			foreach (int utility in new[] { ProjectileID.FlyingPiggyBank, ModContent.Find<ModProjectile>("Fargowiltas/SpawnProj").Type })
			{
				Main.projectile = Enumerable.Range(0, Main.maxProjectiles + 1).Select(i => new Projectile { whoAmI = i }).ToArray();
				Item item = new(); item.SetDefaults(utility == ProjectileID.FlyingPiggyBank ? ItemID.MoneyTrough : ModContent.Find<ModItem>("Fargowiltas/Abeemination2").Type);
				Projectile.NewProjectile(player.GetSource_ItemUse(item), player.Center, Vector2.Zero, utility, 0, 0, player.whoAmI, utility == ProjectileID.FlyingPiggyBank ? 0 : NPCID.QueenBee);
				if (Main.projectile.Count(p => p.active) != 1) throw new InvalidOperationException("Danmaku duplicated utility " + utility);
				checks++;
			}
			return checks;
		}
		finally { Main.projectile = savedProjectiles; count.SetValue(settings, savedCount); enabled.SetValue(settings, savedEnabled); }
	}
	private static void TraceDefaults(List<object> observations, List<ILHook> hooks)
	{
		bool noticed = false;
		string previous = null;
		void Notice(string location)
		{
			if (!noticed && Main.npc[0].active) { noticed = true; observations.Add(new { stage = "first-slot0-active", method = location, previous, stack = Environment.StackTrace }); }
			previous = location;
		}
		var methods = ModLoader.Mods.SelectMany(m => m.GetContent<GlobalNPC>()).SelectMany(g => g.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(m => m.Name is "SetDefaults" or "OnSpawn"))
			.Concat(ModLoader.Mods.SelectMany(m => m.GetContent<GlobalProjectile>()).SelectMany(g => g.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(m => m.Name == "OnKill")))
			.Append(ModContent.Find<ModProjectile>("Fargowiltas/SpawnProj").GetType().GetMethod("OnKill"))
			.Concat(typeof(NPC).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Where(m => m.Name is "NewNPC" or "SetDefaults"));
		methods = methods.Concat(typeof(Projectile).GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m => m.Name == "NewProjectile" && m.GetParameters().Length == 12));
		foreach (MethodInfo method in methods.Distinct())
		{
			hooks.Add(new ILHook(method, il => {
				var begin = new ILCursor(il);
				begin.EmitDelegate<Action>(() => Notice(method.DeclaringType.FullName + ":" + method.Name + ":entry"));
				if (method.DeclaringType == typeof(Projectile))
				{
					begin.Emit(OpCodes.Ldarg, 5); begin.EmitDelegate<Action<int>>(type => { if (type == ModContent.Find<ModProjectile>("Fargowiltas/SpawnProj").Type) observations.Add(new { stage = "native-service-projectile-entry", stack = Environment.StackTrace }); });
				}
				if (method.DeclaringType == typeof(NPC) && method.Name == "NewNPC")
				{
					begin.Emit(OpCodes.Ldarg_3); begin.EmitDelegate<Action<int>>(type => observations.Add(new { stage = "native-NewNPC-entry", type, stack = Environment.StackTrace }));
					var write = new ILCursor(il);
					if (write.TryGotoNext(MoveType.Before, i => i.MatchStfld<Entity>(nameof(Entity.active))))
					{
						write.Remove(); write.EmitDelegate<Action<Entity, bool>>((entity, value) => { observations.Add(new { stage = "native-active-write", entity.whoAmI, type = (entity as NPC)?.type, value, beforeSlot0 = Main.npc[0].active }); entity.active = value; });
					}
				}
				foreach (var instruction in il.Body.Instructions.Where(i => i.OpCode == OpCodes.Ret).ToArray())
				{
					var cursor = new ILCursor(il); cursor.Goto(instruction, MoveType.Before);
					cursor.EmitDelegate<Action>(() => Notice(method.DeclaringType.FullName + ":" + method.Name + ":exit"));
				}
			}));
		}
	}
	private static void Sample(string stage, Player player, List<object> observations)
	{
		foreach (int type in new[] { ProjectileID.WoodenArrowFriendly, ProjectileID.Stinger, ProjectileID.FlyingPiggyBank })
		{
			int slot = Projectile.NewProjectile(player.GetSource_Misc("DingDongCombatProbe"), player.Center + new Vector2(200, 0), new Vector2(2, 0), type, type == ProjectileID.FlyingPiggyBank ? 0 : 10, 0, player.whoAmI);
			Projectile projectile = Main.projectile[slot];
			bool created = projectile.active; int lifetime = projectile.timeLeft;
			string error = null;
			try { for (int tick = 0; tick < 3; tick++) projectile.Update(slot); }
			catch (Exception exception) { error = exception.ToString(); }
			observations.Add(new { stage, type, slot, created, active = projectile.active, lifetimeBefore = lifetime, lifetimeAfter = projectile.timeLeft, x = projectile.position.X, y = projectile.position.Y, error });
			projectile.active = false;
		}
	}
	private static void ObserveBoss(string stage, List<object> observations)
	{
		var globals = typeof(NPC).GetField("_globals", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		observations.Add(new { stage, bosses = Main.npc.Where(n => n.active).Select(n => new { n.type, n.whoAmI, globals = ((Array)globals.GetValue(n))?.Cast<object>().Select(g => g?.GetType().FullName).ToArray() }).ToArray() });
	}
	private void Write(object value)
	{
		string output = Environment.GetEnvironmentVariable("DINGDONG_COMBAT_PROBE_OUTPUT");
		if (string.IsNullOrWhiteSpace(output)) throw new InvalidOperationException("Disposable combat output must be explicit.");
		File.WriteAllText(output, JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }));
		Mod.Logger.Info("DINGDONG_COMBAT_PROBE|receipt=" + output);
	}
}
