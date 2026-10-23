using System;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.ID;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
namespace DingDongBigProbe;

public sealed class ProbeSystem : ModSystem
{
 private readonly List<object> results=new();
 private void Check(bool ok,string name,object evidence=null){results.Add(new{name,status=ok?"PASS":"FAIL",evidence});if(!ok)throw new Exception(name);}
 public override void PostAddRecipes()
 {
  string root=Environment.GetEnvironmentVariable("DINGDONG_BIG_NATIVE_PROBE_ROOT");
  if(root==null)return;
  if(!Main.dedServ || !Path.GetFullPath(root).Equals(@"D:\SteamLibrary\CodexWork\dingdong-big-rc-20261023",StringComparison.OrdinalIgnoreCase))throw new Exception("Probe isolation boundary");
  int exit=1;string error=null;
  try
  {
   Main.netMode=0;Main.myPlayer=0;Main.maxTilesX=128;Main.maxTilesY=128;
   Main.tile=(Tilemap)Activator.CreateInstance(typeof(Tilemap),BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,new object[]{(ushort)128,(ushort)128},null);
   var player=Main.player[0]=new Player();player.whoAmI=0;player.active=true;player.selectedItem=0;
   var owner=ModLoader.GetMod("DingDongBiomeHomes");
   Type balance=owner.Code.GetType("DingDongBiomeHomes.Compatibility.SignatureBalanceSystem",true);
   var apply=balance.GetMethod("Apply",BindingFlags.Static|BindingFlags.NonPublic);
   Main.hardMode=true;NPC.downedBoss2=true;NPC.downedPlantBoss=true;NPC.downedMoonlord=true;apply.Invoke(null,new object[]{true});
   var sig=ModLoader.GetMod("SignatureEquipment");
   Type si=sig.Code.GetType("SignatureEquipment.SignatureGlobalItem",true),sp=sig.Code.GetType("SignatureEquipment.SignatureGlobalProjectile",true);
   var sit=sig.GetContent<GlobalItem>().Single(g=>g.GetType()==si);
   var spt=sig.GetContent<GlobalProjectile>().Single(g=>g.GetType()==sp);
   const BindingFlags fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
   var level=si.GetField("signatureLevel",fields);var xp=si.GetField("signatureExpCurrent",fields);
   var we=ModLoader.GetMod("WeaponEnchantments");Type wt=we.Code.GetType("WeaponEnchantments.Common.Globals.EnchantedWeapon",true);
   var wet=we.GetContent<GlobalItem>().Single(g=>g.GetType()==wt);
   var infusion=wt.GetField("infusionDamageMultiplier",fields);
   var weapon=new Item(ItemID.Minishark);player.inventory[0]=weapon;
   var state=weapon.GetGlobalItem(sit);var westate=weapon.GetGlobalItem(wet);
   infusion.SetValue(westate,2f);level.SetValue(state,0);xp.SetValue(state,123);
   int initial=player.GetWeaponDamage(weapon);
   level.SetValue(state,100);int at100=player.GetWeaponDamage(weapon);
   level.SetValue(state,1000);int at1000=player.GetWeaponDamage(weapon);
   Check(initial>0 && at100>=initial && at100==at1000,"Signature saturates while native WE infusion remains active",new{initial,at100,at1000});
   Check((float)si.GetProperty("AdditionalProjectileChanceCapped",fields).GetValue(state)>100,"Projectile quantity progression remains uncapped beyond combat ceiling");
   Main.hardMode=false;NPC.downedBoss2=false;NPC.downedPlantBoss=false;NPC.downedMoonlord=false;apply.Invoke(null,new object[]{true});
   Check((int)si.GetProperty("DamageNotCapped",fields).GetValue(state)==10,"Item-specific hard caps cannot bypass the current combat stage");
   Main.hardMode=true;NPC.downedBoss2=true;NPC.downedPlantBoss=true;NPC.downedMoonlord=true;apply.Invoke(null,new object[]{true});
   var cloned=weapon.Clone();Check((int)level.GetValue(cloned.GetGlobalItem(sit))==1000,"Clone preserves mastery");
   var loaded=ItemIO.Load(ItemIO.Save(weapon));Check((int)level.GetValue(loaded.GetGlobalItem(sit))==1000,"Item save/load preserves mastery");
   using(var buffer=new MemoryStream())
   {
    using(var w=new BinaryWriter(buffer,System.Text.Encoding.UTF8,true))ItemIO.Send(weapon,w,true,true);
    buffer.Position=0;using var r=new BinaryReader(buffer);
    var received=ItemIO.Receive(r,true,true);
    Check(buffer.Position==buffer.Length && (int)level.GetValue(received.GetGlobalItem(sit))==1000,"Native item network roundtrip consumes exactly its payload",new{bytes=buffer.Length});
   }
   var staff=new Item(ItemID.SlimeStaff);player.inventory[0]=staff;
   var staffState=staff.GetGlobalItem(sit);level.SetValue(staffState,1000);xp.SetValue(staffState,123);
   Projectile Make(bool minion,int penetrate=1){var p=new Projectile();p.SetDefaults(ProjectileID.WoodenArrowFriendly);p.owner=0;p.damage=42;p.penetrate=p.maxPenetrate=penetrate;p.timeLeft=30;p.minion=minion;p.minionSlots=minion?1:0;return p;}
   var spawn=sp.GetMethod("OnSpawn");
   void Spawn(Projectile p,IEntitySource source)=>spawn.Invoke(p.GetGlobalProjectile(spt),new object[]{p,source});
   var parent=Make(true);Spawn(parent,new EntitySource_ItemUse(player,staff));
   var child=Make(true);Spawn(child,new EntitySource_Parent(parent));
   float expected=.1f/5.1f;
   Check(Math.Abs(parent.minionSlots-expected)<1e-5f && Math.Abs(child.minionSlots-expected)<1e-5f,"Item and child minion routes use one positive unbounded-discount formula",new{parent=parent.minionSlots,child=child.minionSlots});
   Check(parent.penetrate<=2 && child.penetrate<=2 && parent.timeLeft<=60 && child.timeLeft<=60,"Native spawn routes obey relative 2x penetration/lifetime limits",new{parent.penetrate,parent.timeLeft,childPenetrate=child.penetrate,childLifetime=child.timeLeft});
   var infinite=Make(false,-1);Spawn(infinite,new EntitySource_ItemUse(player,staff));
   Check(infinite.penetrate==-1,"Native infinite penetration is retained");
   var hit=sp.GetMethod("ModifyHitNPC");int before=parent.damage;
   for(int i=0;i<5;i++){var modifiers=new NPC.HitModifiers();hit.Invoke(parent.GetGlobalProjectile(spt),new object[]{parent,new NPC(),modifiers});}
   Check(parent.damage==before,"Signature hit does not overwrite native minion/WE spawn damage",new{before,after=parent.damage});
   Type configType=sig.Code.GetType("SignatureEquipment.Configs.SignatureEquipmentServerConfig",true);
   var instance=typeof(ModContent).GetMethods().Single(m=>m.Name=="GetInstance" && m.IsGenericMethodDefinition && m.GetParameters().Length==0).MakeGenericMethod(configType).Invoke(null,null);
   var inherits=(HashSet<Terraria.ModLoader.Config.ProjectileDefinition>)configType.GetField("ProjectilesInheritItemDamage").GetValue(instance);
   var arrow=new Terraria.ModLoader.Config.ProjectileDefinition(parent.type);bool added=inherits.Add(arrow);
   sp.GetField("itemWeaponDamage",fields).SetValue(parent.GetGlobalProjectile(spt),100);
   for(int i=0;i<100;i++){var modifiers=new NPC.HitModifiers();hit.Invoke(parent.GetGlobalProjectile(spt),new object[]{parent,new NPC(),modifiers});}
   Check(parent.damage==before,"Legacy inherit-damage projectiles cannot compound damage across repeated hits",new{before,after=parent.damage,hits=100});
   if(added)inherits.Remove(arrow);
   Check((int)level.GetValue(staffState)==1000 && (int)xp.GetValue(staffState)==123,"Balance retains accumulated mastery and XP");
   Experience(player,owner,sig,sit,si,level,xp,apply);
   Gravity(player);
   exit=0;
  }
  catch(Exception ex){error=ex.ToString();}
  File.WriteAllText(Path.Combine(root,"Evidence","native-integration-result.json"),JsonSerializer.Serialize(new{status=exit==0?"PASS":"FAIL",results,error,worldLoaded=false,userSavesUsed=false},new JsonSerializerOptions{WriteIndented=true}));
  Environment.Exit(exit);
 }
 private void Experience(Player player,Mod owner,Mod sig,GlobalItem sit,Type si,FieldInfo level,FieldInfo xp,MethodInfo apply)
 {
  Type type=sig.Code.GetType("SignatureEquipment.SignatureEquipmentPlayer",true);
  var template=sig.GetContent<ModPlayer>().Single(g=>g.GetType()==type);
  var state=player.GetModPlayer(template);
  var before=type.GetMethod("OnModifyHit",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
  var after=type.GetMethod("OnHit",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
  var target=new NPC();target.SetDefaults(NPCID.BlueSlime);target.active=true;
  var clock=typeof(Main).GetField("_gameUpdateCount",BindingFlags.Static|BindingFlags.NonPublic);
  int Simulate(int raw,int stage,bool melee,int hitsPerWindow)
  {
   Main.hardMode=stage==100;NPC.downedBoss2=stage==100;NPC.downedPlantBoss=stage==100;NPC.downedMoonlord=stage==100;apply.Invoke(null,new object[]{true});
   owner.Code.GetType("DingDongBiomeHomes.Compatibility.SignatureExperienceCompatibility",true).GetMethod("Reset",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
   var weapon=new Item(melee?ItemID.CopperBroadsword:ItemID.SlimeStaff);player.inventory[0]=weapon;
   var data=weapon.GetGlobalItem(sit);level.SetValue(data,raw);xp.SetValue(data,0);
   for(int tick=0;tick<600;tick+=10)
   {
    clock.SetValue(null,(uint)(10000+tick));
    for(int n=0;n<hitsPerWindow;n++){before.Invoke(state,new object[]{weapon,target});after.Invoke(state,new object[]{data,weapon,1,target});}
   }
   return (int)xp.GetValue(data)+((int)level.GetValue(data)-raw)*500;
  }
  int solo=Simulate(0,20,false,1),army=Simulate(0,20,false,81),melee=Simulate(0,20,true,1),veteran=Simulate(178,100,false,81);
  Check(solo==army && melee>solo && veteran<solo && veteran>0,"Actual Signature hit XP is time-based: 81 summons share one budget, melee/early gear catch up",new{solo,army,melee,veteran,seconds=10});
 }
 private void Gravity(Player player)
 {
  var provider=ModLoader.GetMod("CalamityMod");
  var t=provider.Code.GetType("CalamityMod.Systems.Mechanic.GravityManipulationChangesSystem",true);
  var delay=t.GetMethod("DelayGravity",BindingFlags.Static|BindingFlags.NonPublic);
  var cp=provider.Code.GetType("CalamityMod.CalPlayer.CalamityPlayer",true);
  var template=provider.GetContent<ModPlayer>().Single(g=>g.GetType()==cp);
  var state=player.GetModPlayer(template);
  var changed=cp.GetField("justChangedGravity",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);var old=cp.GetField("oldGravDir",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
  var hooks=new List<ILHook>();
  try
  {
   // Simulated assigned key and no physical keypress. The native provider body
   // and our compatibility patch execute unchanged around these input seams.
   hooks.Add(new ILHook(delay,il=>{var c=new ILCursor(il);if(!c.TryGotoNext(MoveType.After,i=>i.Operand is MethodReference m && m.Name=="get_Count"))throw new Exception("gravity input seam");c.Emit(OpCodes.Pop);c.Emit(OpCodes.Ldc_I4_1);}));
   var pressed=Terraria.GameInput.PlayerInput.Triggers.JustPressed.KeyStatus;
   pressed["CalamityMod/GravitySwapOverride"]=false;
   player.gravControl=true;player.gravControl2=false;player.controlUp=true;player.releaseUp=true;player.forcedGravity=0;
   player.gravDir=-1;changed.SetValue(state,false);old.SetValue(state,1f);
   On_Player.orig_UpdateControlHolds noOp=p=>{};
   delay.Invoke(null,new object[]{noOp,player});
   player.controlUp=false;player.releaseUp=false;
   delay.Invoke(null,new object[]{noOp,player});
   Check(player.gravDir==-1,"Vanilla Up toggle survives a separate Calamity hotkey",new{player.gravDir});
   player.gravControl=false;player.gravControl2=false;player.controlUp=true;player.releaseUp=true;player.gravDir=1;
   changed.SetValue(state,false);old.SetValue(state,1f);
   delay.Invoke(null,new object[]{noOp,player});
   Check(player.gravDir==1,"No gravity ability is granted when its flags are disabled");
   player.gravControl=true;player.controlUp=false;player.releaseUp=false;player.gravDir=1;changed.SetValue(state,false);old.SetValue(state,1f);pressed["CalamityMod/GravitySwapOverride"]=true;
   delay.Invoke(null,new object[]{noOp,player});pressed["CalamityMod/GravitySwapOverride"]=false;delay.Invoke(null,new object[]{noOp,player});
   Check(player.gravDir==-1,"The separate Calamity hotkey still toggles gravity");
   player.controlUp=true;player.releaseUp=true;player.gravDir=1;player.forcedGravity=1;changed.SetValue(state,false);old.SetValue(state,1f);
   delay.Invoke(null,new object[]{noOp,player});player.controlUp=false;player.releaseUp=false;delay.Invoke(null,new object[]{noOp,player});
   Check(player.gravDir==-1,"Provider forced-gravity effect is retained alongside Up input");
  }
  finally{for(int i=hooks.Count-1;i>=0;i--)hooks[i].Dispose();}
 }
}
