using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;

internal sealed class ResidentEncounterState
{
    internal bool Found;
    internal Point Day, Night;
    internal bool NightActive;
}

// World-owned discovery is separate from reputation and the later player-house move.
public sealed class ResidentEncounterSystem : ModSystem
{
    private static readonly Dictionary<string,ResidentEncounterState> States=new(StringComparer.Ordinal);
    private static readonly Dictionary<string,int> RespawnDelay=new(StringComparer.Ordinal);
    private static readonly Dictionary<string,int> AnchorRetry=new(StringComparer.Ordinal);
    private static readonly HashSet<string> Warned=new(StringComparer.Ordinal);
    private static bool initialized;
    internal static uint Revision {get;private set;}
    private static bool Enabled => ModContent.GetInstance<DingDongServerConfig>().EnableNpcQuestsAndEscort && SettlementWorldState.HasCompleteResidenceRegistry;
    internal static bool IsPending(string key) => initialized && States.TryGetValue(key,out var s) && !s.Found;
    internal static bool CanOfferQuests(string key) => !IsPending(key);
    internal static bool TryState(string key,out ResidentEncounterState state)=>States.TryGetValue(key,out state);
    public override void ClearWorld(){States.Clear();RespawnDelay.Clear();Warned.Clear();AnchorRetry.Clear();ResidentEncounterTerrain.Reset();initialized=false;}
    public override void PostWorldGen(){if(Enabled) { Initialize(false);Maintain(); }}
    public override void PostUpdateWorld()
    {
        if(Main.netMode==NetmodeID.MultiplayerClient||Main.gameMenu||!Enabled||Main.GameUpdateCount%60!=0)return;
        if(!initialized)Initialize(true);
        Maintain();
    }
    private static void Initialize(bool preserveExisting)
    {
        foreach(var id in NpcTownManifest.All)
        {
            if(States.ContainsKey(id.NpcKey))continue;
            bool found=!Special(id.NpcKey)||NativeUnlocked(id.NpcKey);
            if(preserveExisting && FindFinal(id.NpcKey)!=null)found=true;
            States[id.NpcKey]=new(){Found=found};
        }
        initialized=true;
        if(Main.netMode==NetmodeID.Server)Broadcast();
    }
    private static bool Special(string key)=>key is "Terraria/Clothier" or "Terraria/Dryad" or "Terraria/GoblinTinkerer" or "Terraria/Wizard" or "Terraria/Mechanic" or "Terraria/Stylist" or "Terraria/Angler" or "Terraria/DD2Bartender" or "Terraria/Golfer" or "Terraria/TaxCollector" or "Terraria/Steampunker" or "Terraria/SantaClaus";
    private static bool NativeUnlocked(string key)=>key switch {
        "Terraria/Clothier"=>NPC.downedBoss3,
        "Terraria/Dryad"=>QuestWorldProgressSystem.DefeatedBossCount>0||NPC.downedBoss1||NPC.downedBoss2||NPC.downedBoss3||NPC.downedSlimeKing||NPC.downedQueenBee,
        "Terraria/GoblinTinkerer"=>NPC.savedGoblin,"Terraria/Wizard"=>NPC.savedWizard,"Terraria/Mechanic"=>NPC.savedMech,
        "Terraria/Stylist"=>NPC.savedStylist,"Terraria/Angler"=>NPC.savedAngler,"Terraria/DD2Bartender"=>NPC.savedBartender,"Terraria/Golfer"=>NPC.savedGolfer,
        _=>false };
    internal static int PendingType(string key)=>key switch {
        "Terraria/Clothier"=>NPCID.OldMan,"Terraria/GoblinTinkerer"=>NPCID.BoundGoblin,"Terraria/Wizard"=>NPCID.BoundWizard,
        "Terraria/Mechanic"=>NPCID.BoundMechanic,"Terraria/Stylist"=>NPCID.WebbedStylist,"Terraria/Angler"=>NPCID.SleepingAngler,
        "Terraria/DD2Bartender"=>NPCID.BartenderUnconscious,"Terraria/Golfer"=>NPCID.GolferRescue,"Terraria/TaxCollector"=>NPCID.DemonTaxCollector,
        _=>NpcResidentRegistry.TryGetType(key,out int t)?t:0 };
    internal static bool TryKey(NPC npc,out string key)
    {
        if(NpcResidentRegistry.TryGet(npc,out key,out _))return true;
        key=npc.type switch { NPCID.OldMan=>"Terraria/Clothier",NPCID.BoundGoblin=>"Terraria/GoblinTinkerer",NPCID.BoundWizard=>"Terraria/Wizard",NPCID.BoundMechanic=>"Terraria/Mechanic",NPCID.WebbedStylist=>"Terraria/Stylist",NPCID.SleepingAngler=>"Terraria/Angler",NPCID.BartenderUnconscious=>"Terraria/DD2Bartender",NPCID.GolferRescue=>"Terraria/Golfer",NPCID.DemonTaxCollector=>"Terraria/TaxCollector",_=>"" };
        return key.Length>0;
    }
    private static NPC FindFinal(string key)
    {if(!NpcResidentRegistry.TryGetType(key,out int t))return null;foreach(var n in Main.ActiveNPCs)if(n.type==t)return n;return null;}
    private static NPC FindType(int type){foreach(var n in Main.ActiveNPCs)if(n.type==type)return n;return null;}
    internal static void Died(string key){if(initialized)RespawnDelay[key]=600;}
    private static void Maintain()
    {
        bool changed=false;
        foreach(var id in NpcTownManifest.All)
        {
            string key=id.NpcKey;var s=States[key];
            if(!SettlementWorldState.TryGetResidence(key,out var residence)||!NpcResidentRegistry.TryGetType(key,out int final))continue;
            int initial=PendingType(key);NPC current=FindFinal(key);
            if(!s.Found && (NativeUnlocked(key)||(initial!=final&&current!=null)))
            {s.Found=true;changed=true;var old=FindType(initial);if(current==null&&old!=null){old.Transform(final);current=old;}if(current!=null)ReturnHome(current,key,residence);}
            int desired=s.Found?final:initial;
            current??=FindType(desired);
            if(current!=null && current.type!=desired)continue;
            if(current==null)
            {
                if(AnchorRetry.TryGetValue(key,out int retry)&&retry>0){AnchorRetry[key]=Math.Max(0,retry-60);continue;}
                if(RespawnDelay.TryGetValue(key,out int delay)&&delay>0){RespawnDelay[key]=Math.Max(0,delay-60);continue;}
                Point point;
                if(s.Found){if(!TryHomePoint(key,residence,desired,out point)){Warn(key+":home");AnchorRetry[key]=3600;continue;}}
                else
                {
                    if(s.Day==Point.Zero&&!ResidentEncounterTerrain.TryAnchor(key,residence,false,out s.Day)) {Warn(key);AnchorRetry[key]=3600;continue;}
                    if(key=="Terraria/Dryad"&&s.Night==Point.Zero&&!ResidentEncounterTerrain.TryAnchor(key,residence,true,out s.Night)){Warn(key+":night");AnchorRetry[key]=3600;continue;}
                    point=key=="Terraria/Dryad"&&!Main.dayTime?s.Night:s.Day;s.NightActive=!Main.dayTime;
                }
                if(!ResidentEncounterTerrain.TryCenter(point,desired,out var center)){Warn(key+":clearance");continue;}
                int index=NPC.NewNPC(new EntitySource_Misc("DingDongResidentEncounter"),(int)center.X,(int)center.Y,desired);
                if(index>=Main.maxNPCs)continue;
                current=Main.npc[index];changed=true;current.Center=center;current.netUpdate=true;
                // NewNPC leaves homeless=false with home (-1,-1); vanilla night AI would teleport there.
                current.homeless=true;current.homeTileX=current.homeTileY=-1;
                if(s.Found)NpcResidenceWorldSystem.ApplyLockedHome(current,key);
            }
            if(!s.Found)
            {
                // Pending town forms must not use an undiscovered residence as a teleport destination.
                current.homeless=true;current.homeTileX=current.homeTileY=-1;
                if(key=="Terraria/Dryad"&&s.NightActive==Main.dayTime)
                {s.NightActive=!Main.dayTime;var p=Main.dayTime?s.Day:s.Night;if(ResidentEncounterTerrain.TryCenter(p,current.type,out var center)){current.Center=center;current.velocity=Vector2.Zero;current.netUpdate=true;}changed=true;}
            }
            else
            {
                var escort=NpcResidenceWorldSystem.GetState(key);
                if(escort.Stage is ResidentEscortStage.Following or ResidentEscortStage.Waiting or ResidentEscortStage.AwaitingHome)continue;
                if(TryHomePoint(key,residence,current.type,out var point)&&ResidentEncounterTerrain.TryCenter(point,current.type,out var center)&&Vector2.DistanceSquared(center,current.Center)>256*256&&NoPlayerNear(current.Center,1600))ReturnHome(current,key,residence);
            }
        }
        if(changed&&Main.netMode==NetmodeID.Server)Broadcast();
    }
    private static bool TryHomePoint(string key,SettlementResidenceReport residence,int type,out Point point)
    {
        var state=NpcResidenceWorldSystem.GetState(key);
        if(NpcResidenceWorldSystem.IsOriginReleased(key)&&state.DestinationHome.X>0)
        {return ResidentEncounterTerrain.TryAssignedHomeAnchor(state.DestinationHome,type,out point);}
        return ResidentEncounterTerrain.TryHomeAnchor(residence,type,out point);
    }
    private static bool NoPlayerNear(Vector2 position,int radius){foreach(var p in Main.ActivePlayers)if(Vector2.DistanceSquared(position,p.Center)<radius*radius)return false;return true;}
    private static void ReturnHome(NPC npc,string key,SettlementResidenceReport residence)
    {if(!TryHomePoint(key,residence,npc.type,out var point)||!ResidentEncounterTerrain.TryCenter(point,npc.type,out var center))return;npc.Center=center;npc.velocity=Vector2.Zero;npc.dontTakeDamage=Terraria.ID.ContentSamples.NpcsByNetId[npc.type].dontTakeDamage;NpcResidenceWorldSystem.ApplyLockedHome(npc,key);npc.netUpdate=true;}
    private static void Warn(string key){if(Warned.Add(key))DingDongBiomeHomes.Instance.Logger.Warn("[ResidentEncounter] No safe biome anchor: "+key);}
    internal static void Talk(Player player,int npcIndex)
    {
        if(Main.netMode==NetmodeID.MultiplayerClient||!initialized||player==null||!player.active||player.dead||npcIndex<0||npcIndex>=Main.maxNPCs)return;
        var npc=Main.npc[npcIndex];if(!npc.active||Vector2.DistanceSquared(player.Center,npc.Center)>224*224||!TryKey(npc,out string key)||!IsPending(key))return;
        if(key is not ("Terraria/SantaClaus" or "Terraria/Steampunker"))return;
        States[key].Found=true;npc.dontTakeDamage=false;npc.netUpdate=true;
        if(Main.netMode==NetmodeID.Server)Broadcast();
    }
    private static void Broadcast()
    {
        Revision++;
        if(Main.netMode!=NetmodeID.Server)return;
        foreach(var player in Main.ActivePlayers)
        {
            if(!NpcQuestNetwork.CanSendToClient(player.whoAmI))continue;
            var packet=DingDongBiomeHomes.Instance.GetPacket();packet.Write((byte)DingDongPacketKind.ResidentEncounterSnapshot);packet.Write((byte)1);
            ModContent.GetInstance<ResidentEncounterSystem>().NetSend(packet);packet.Send(player.whoAmI);
        }
    }
    internal static void RequestTalk(NPC npc)
    {
        if(Main.netMode==NetmodeID.SinglePlayer){Talk(Main.LocalPlayer,npc.whoAmI);return;}
        if(Main.netMode!=NetmodeID.MultiplayerClient)return;
        var p=DingDongBiomeHomes.Instance.GetPacket();p.Write((byte)DingDongPacketKind.ResidentEncounterTalk);p.Write((short)npc.whoAmI);p.Send();
    }
    public override void SaveWorldData(TagCompound tag)
    {tag["ResidentEncountersVersion"]=1;tag["ResidentEncountersInitialized"]=initialized;tag["ResidentEncounters"]=States.Select(p=>new TagCompound{["Npc"]=p.Key,["Found"]=p.Value.Found,["DayX"]=p.Value.Day.X,["DayY"]=p.Value.Day.Y,["NightX"]=p.Value.Night.X,["NightY"]=p.Value.Night.Y,["NightActive"]=p.Value.NightActive}).ToList();}
    public override void LoadWorldData(TagCompound tag)
    {States.Clear();initialized=tag.GetBool("ResidentEncountersInitialized");if(tag.GetInt("ResidentEncountersVersion")!=1){initialized=false;return;}foreach(var t in tag.GetList<TagCompound>("ResidentEncounters")){string k=t.GetString("Npc");if(NpcTownManifest.TryGet(k,out _))States[k]=new(){Found=t.GetBool("Found"),Day=new(t.GetInt("DayX"),t.GetInt("DayY")),Night=new(t.GetInt("NightX"),t.GetInt("NightY")),NightActive=t.GetBool("NightActive")};}if(States.Count!=NpcTownManifest.AuthoredCount)initialized=false;}
    public override void NetSend(BinaryWriter w){w.Write(initialized);w.Write((byte)States.Count);foreach(var p in States){w.Write(p.Key);w.Write(p.Value.Found);w.Write(p.Value.Day.X);w.Write(p.Value.Day.Y);w.Write(p.Value.Night.X);w.Write(p.Value.Night.Y);w.Write(p.Value.NightActive);}}
    public override void NetReceive(BinaryReader r)
    {bool ready=r.ReadBoolean();int count=r.ReadByte();if(count>NpcTownManifest.AuthoredCount)throw new InvalidDataException("Too many resident encounters");var next=new Dictionary<string,ResidentEncounterState>(StringComparer.Ordinal);for(int i=0;i<count;i++){string key=r.ReadString();var s=new ResidentEncounterState{Found=r.ReadBoolean(),Day=new(r.ReadInt32(),r.ReadInt32()),Night=new(r.ReadInt32(),r.ReadInt32()),NightActive=r.ReadBoolean()};if(!NpcTownManifest.TryGet(key,out _)||!next.TryAdd(key,s))throw new InvalidDataException("Invalid encounter identity");}States.Clear();foreach(var p in next)States.Add(p.Key,p.Value);initialized=ready&&count==NpcTownManifest.AuthoredCount;Revision++;}
}
