using System;
using System.Collections.Generic;
using System.IO;
namespace DingDongBiomeHomes;
internal static partial class NpcQuestNetwork
{
    internal static void WriteSharedHistory(BinaryWriter w)
    {
        w.Write((byte)NpcQuestWorldLedger.History.Count);
        foreach(var h in NpcQuestWorldLedger.History)
        { w.Write(h.NpcKey); w.Write(h.QuestId);w.Write(h.Ordinal);w.Write(h.Performer);w.Write(h.Coins);w.Write(h.RewardType);w.Write(h.RewardCount); }
    }
    internal static List<SharedQuestCompletion> ReadSharedHistory(BinaryReader r)
    {
        int count=r.ReadByte();if(count>64)throw new InvalidDataException("Quest history limit exceeded");
        var list=new List<SharedQuestCompletion>(count);
        for(int i=0;i<count;i++)
        {
            var h=new SharedQuestCompletion(r.ReadString(),r.ReadInt32(),r.ReadInt32(),r.ReadString(),r.ReadInt32(),r.ReadInt32(),r.ReadInt32());
            if(!NpcTownManifest.TryGet(h.NpcKey,out _)||h.Performer.Length>60||h.Ordinal<1||h.QuestId<0||h.Coins<0||h.RewardCount<0)throw new InvalidDataException("Invalid quest history entry");
            list.Add(h);
        }
        return list;
    }
}
