using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;

// A projectile fired by a resident may hurt a player only while that player's reputation with
// the resident is at the hostile line; everyone else is untouched. The mark travels with the
// projectile to clients, where player hits are decided.
public sealed class ResidentAttackProjectile : GlobalProjectile
{
    private string npcKey;
    public override bool InstancePerEntity => true;

    public override void OnSpawn(Projectile projectile, IEntitySource source)
    {
        if (source is not EntitySource_Parent { Entity: NPC npc } || !NpcResidentRegistry.TryGet(npc, out string key, out _)) return;
        foreach (Player player in Main.ActivePlayers)
            if (ResidentReputationGlobalNPC.IsHostileTo(player, key)) { Mark(projectile, key); return; }
    }

    private void Mark(Projectile projectile, string key)
    {
        npcKey = key;
        projectile.hostile = true;
        projectile.netUpdate = true;
    }

    public override bool CanHitPlayer(Projectile projectile, Player target) =>
        npcKey == null || ResidentReputationGlobalNPC.IsHostileTo(target, npcKey);

    public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
    {
        bitWriter.WriteBit(npcKey != null);
        if (npcKey != null) binaryWriter.Write(npcKey);
    }

    public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
    {
        if (bitReader.ReadBit()) Mark(projectile, binaryReader.ReadString());
    }
}
