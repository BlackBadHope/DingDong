using System;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace DingDongBiomeHomes.Diagnostics;

// Observation only: do not override a real boss gate or the owner's dummy settings.
public sealed class DummyStateTrace : GlobalNPC
{
 public override bool InstancePerEntity => true;
 private int previous;
 public override bool AppliesToEntity(NPC entity, bool lateInstantiation) =>
  entity.ModNPC?.GetType().FullName == "ImproveGame.Content.NPCs.Dummy.DummyNPC";
 public override void PostAI(NPC npc) {
  int state = (npc.dontTakeDamage ? 1 : 0) | (npc.friendly ? 2 : 0) | (npc.immortal ? 4 : 0) |
   (!npc.chaseable ? 8 : 0) | (npc.life <= 0 ? 16 : 0) | (!npc.active ? 32 : 0) | (!npc.CanBeChasedBy() ? 64 : 0);
  if (state == previous) return;
  int old = previous; previous = state;
  string bosses = string.Join(",", Main.npc.Take(Main.maxNPCs).Where(n => n != null && n.active &&
   (n.boss || (uint)n.type < NPCID.Sets.ShouldBeCountedAsBoss.Length && NPCID.Sets.ShouldBeCountedAsBoss[n.type]))
   .Select(n => n.whoAmI + ":" + (n.ModNPC?.FullName ?? n.type.ToString())));
  string buffs = string.Join(",", npc.buffType.Where((type, i) => type > 0 && npc.buffTime[i] > 0));
  Mod.Logger.Info($"DINGDONG_DUMMY_STATE|npc={npc.whoAmI}|tick={Main.GameUpdateCount}|before={old}|state={state}|life={npc.life}/{npc.lifeMax}|ai={npc.aiStyle}|bosses={bosses}|buffs={buffs}");
 }
}
