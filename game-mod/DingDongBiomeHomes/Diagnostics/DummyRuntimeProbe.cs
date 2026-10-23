using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using DingDongBiomeHomes.Compatibility;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace DingDongBiomeHomes.Diagnostics;
internal static class DummyRuntimeProbe
{
 internal static int Run(Player player)
 {
  string output = Environment.GetEnvironmentVariable("DINGDONG_DUMMY_PROBE_OUTPUT");
  if (string.IsNullOrEmpty(output)) return 0;
  const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
  Mod improve = ModLoader.GetMod("ImproveGame");
  int type = improve.Find<ModNPC>("DummyNPC").Type, checks = 0;
  var results = new List<object>();
  void Require(bool ok, string name) { results.Add(new { name, status = ok ? "PASS" : "FAIL" }); if (!ok) throw new InvalidOperationException(name); checks++; }
  NPC[] saved = Main.npc.ToArray(); bool active = player.active; int net = Main.netMode;
  var clock = typeof(Main).GetField("_gameUpdateCount", flags); object previousTime = clock.GetValue(null);
  object updater = improve.GetContent<ModSystem>().Single(s => s.GetType().FullName == "ImproveGame.Core.SystemUpdater");
  MethodInfo pre = updater.GetType().GetMethod("PreUpdateNPCs", flags);
  string failure = null; int initialXp = 0, finalXp = 0; bool overflow = false;
  try {
   Main.netMode = NetmodeID.SinglePlayer; player.active = true;
   for (int i = 0; i < Main.maxNPCs; i++) Main.npc[i] = new NPC { whoAmI = i };
   NPC dummy = new(); dummy.SetDefaults(type); dummy.active = true; dummy.whoAmI = 0; dummy.position = player.position; Main.npc[0] = dummy;
   object modNpc = dummy.ModNPC; Type dt = modNpc.GetType(); dt.GetField("Owner", flags).SetValue(modNpc, player.whoAmI);
   MethodInfo ai = dt.GetMethod("AI", flags), hit = dt.GetMethod("OnHitByProjectile", flags);
   Projectile projectile = new() { active = true, friendly = true, minion = true, owner = player.whoAmI, damage = 100 };
   Mod signature = ModLoader.GetMod("SignatureEquipment");
   ModPlayer sigPlayer = player.GetModPlayer(signature.GetContent<ModPlayer>().Single(p => p.GetType().Name == "SignatureEquipmentPlayer"));
   MethodInfo before = sigPlayer.GetType().GetMethod("OnModifyHit", flags), after = sigPlayer.GetType().GetMethod("OnHit", flags);
   Item weapon = new(ItemID.SlimeStaff); player.inventory[0] = weapon;
   GlobalItem sigItem = weapon.GetGlobalItem(signature.GetContent<GlobalItem>().Single(g => g.GetType().Name == "SignatureGlobalItem"));
   initialXp = SignatureBalanceSystem.RawLevel(weapon) * 500 + SignatureBalanceSystem.Experience(weapon);
   pre.Invoke(updater, null); ai.Invoke(modNpc, null);
   Require(dummy.CanBeChasedBy(projectile), "Native dummy is a minion target with no boss");
   Require(dummy.TryGetGlobalNPC<DummyStateTrace>(out var trace), "Dummy state trace is attached to the real native dummy type");
   for (int tick = 0; tick < 36000; tick++) {
    clock.SetValue(null, (uint)(100000 + tick));
    pre.Invoke(updater, null); ai.Invoke(modNpc, null);
    if (tick % 10 == 0) {
     RequireState(dummy);
     hit.Invoke(modNpc, new object[] { projectile, new NPC.HitInfo { Damage = 100, Crit = false }, 100 });
     before.Invoke(sigPlayer, new object[] { weapon, dummy }); after.Invoke(sigPlayer, new object[] { sigItem, weapon, 100, dummy });
    }
   }
   finalXp = SignatureBalanceSystem.RawLevel(weapon) * 500 + SignatureBalanceSystem.Experience(weapon);
   Require(finalXp > initialXp && dummy.CanBeChasedBy(projectile) && !dummy.dontTakeDamage, "600 seconds of native dummy callbacks retain minion eligibility and Signature XP");
   NPC boss = new(); boss.SetDefaults(NPCID.KingSlime); boss.active = true; boss.position = player.position + new Microsoft.Xna.Framework.Vector2(300000, 0); Main.npc[1] = boss;
   pre.Invoke(updater, null); ai.Invoke(modNpc, null);
   Require(dummy.dontTakeDamage && dummy.friendly && !dummy.CanBeChasedBy(projectile), "Faraway active boss deliberately disables native dummy");
   trace.PostAI(dummy);
   Require(dummy.dontTakeDamage && dummy.friendly, "Dummy state trace preserves the native boss protection");
   boss.active = false; pre.Invoke(updater, null); ai.Invoke(modNpc, null);
   Require(!dummy.dontTakeDamage && !dummy.friendly && dummy.CanBeChasedBy(projectile), "Boss removal restores native dummy on next update");
   object dps = dt.GetField("DummyDPS", flags).GetValue(modNpc); Type dpt = dps.GetType();
   dpt.GetMethod("Reset", flags).Invoke(dps, null); dpt.GetMethod("Hurt", flags).Invoke(dps, new object[] { int.MaxValue - 10 }); dpt.GetMethod("Hurt", flags).Invoke(dps, new object[] { 100 });
   overflow = (int)dpt.GetField("TotalDamage", flags).GetValue(dps) < 0;
   results.Add(new { name = "Native DPS total uses Int32", overflowReproduced = overflow, doesNotSetNpcImmunity = true });
  } catch (Exception ex) { failure = ex.ToString(); throw; }
  finally {
   for (int i = 0; i < saved.Length; i++) Main.npc[i] = saved[i]; player.active = active; Main.netMode = net; clock.SetValue(null, previousTime);
   File.WriteAllText(output, JsonSerializer.Serialize(new { status = failure == null ? "PASS" : "FAIL", results, initialXp, finalXp, error = failure,
    callbackTicks = 36000, equivalentSeconds = 600, actualWorldSimulation = false, actualMinionAI = false,
    observedIncidentReproduced = false, ownerReport = "No boss active", incidentRootCause = "UNCONFIRMED", nativeDpsOverflow = overflow }));
  }
  return checks;
 }
 private static void RequireState(NPC npc) { if (!npc.active || npc.dontTakeDamage || npc.friendly || npc.immortal || npc.life <= 0) throw new InvalidOperationException("Native dummy became inactive or invulnerable during callback simulation."); }
}
