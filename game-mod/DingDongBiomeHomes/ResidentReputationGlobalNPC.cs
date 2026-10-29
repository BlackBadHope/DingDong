using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

// 30.10 (owner): residents are mini-bosses rather than 100-HP townsfolk, and a resident whose
// reputation with a player fell to the hostile line shoots at that player and can be fought back.
public sealed class ResidentReputationGlobalNPC : GlobalNPC
{
    private static readonly int[] DefenseByTier = { 0, 20, 30, 45, 60 };

    public override void SetDefaults(NPC npc)
    {
        // Content samples are built while loading; only living residents are reshaped.
        if (Main.gameMenu || !NpcResidentRegistry.TryGet(npc, out _, out _)) return;
        int tier = NPC.downedMoonlord ? 4 : NPC.downedPlantBoss ? 3 : Main.hardMode ? 2 : 1;
        npc.lifeMax = Math.Max(npc.lifeMax * 15, 2500) * tier;
        npc.life = npc.lifeMax;
        npc.defense += DefenseByTier[tier];
        npc.damage = Math.Max(npc.damage, 10) * 3;
        npc.knockBackResist = Math.Min(npc.knockBackResist, 0.1f);
    }

    internal static bool IsHostileTo(Player player, string npcKey)
    {
        if (player == null || !player.active) return false;
        int value = Main.netMode == NetmodeID.MultiplayerClient
            ? player.whoAmI == Main.myPlayer ? ResidentReputationSystem.Local(npcKey) : 0
            : ResidentReputationSystem.Get(player.GetModPlayer<NpcQuestPlayer>().PersistentId, npcKey);
        return value <= ResidentReputationSystem.HostileAt;
    }

    public override bool? CanBeHitByItem(NPC npc, Player player, Item item) =>
        NpcResidentRegistry.TryGet(npc, out string key, out _) && IsHostileTo(player, key) ? true : null;

    public override bool? CanBeHitByProjectile(NPC npc, Projectile projectile) =>
        projectile.friendly && projectile.owner >= 0 && projectile.owner < Main.maxPlayers &&
        NpcResidentRegistry.TryGet(npc, out string key, out _) && IsHostileTo(Main.player[projectile.owner], key) ? true : null;

    // Residents keep their own vanilla/mod town-NPC attack: the same projectile they use on
    // monsters is aimed at a player whose standing fell to the hostile line, and those
    // projectiles may hurt that player (ResidentAttackProjectile).
    public override void PostAI(NPC npc)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || !NpcResidentRegistry.TryGet(npc, out string key, out _)) return;
        int projType = FallbackProjectile(npc.type), delay = 30, damage = Math.Max(20, npc.damage);
        float knockback = 2f;
        NPCLoader.TownNPCAttackProj(npc, ref projType, ref delay);
        NPCLoader.TownNPCAttackStrength(npc, ref damage, ref knockback);
        if (projType <= 0 || (Main.GameUpdateCount + (uint)npc.whoAmI) % (uint)Math.Clamp(delay, 20, 90) != 0) return;
        foreach (Player player in Main.ActivePlayers)
        {
            if (player.dead || Vector2.DistanceSquared(player.Center, npc.Center) > 35 * 16 * 35 * 16 || !IsHostileTo(player, key)) continue;
            if (!Collision.CanHitLine(npc.Center, 1, 1, player.Center, 1, 1)) continue;
            Vector2 velocity = Vector2.Normalize(player.Center - npc.Center) * 11f;
            Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, velocity, projType, Math.Max(damage, npc.damage), knockback);
            npc.direction = npc.spriteDirection = Math.Sign(velocity.X);
            break;
        }
    }

    // Vanilla town NPCs choose their projectile inside AI; this mirrors the common ones and
    // falls back by attack style (throw, shoot, magic, melee).
    private static int FallbackProjectile(int type) => type switch
    {
        NPCID.Guide => ProjectileID.WoodenArrowFriendly,
        NPCID.Merchant => ProjectileID.ThrowingKnife,
        NPCID.ArmsDealer or NPCID.Pirate => ProjectileID.Bullet,
        NPCID.Demolitionist => ProjectileID.Grenade,
        NPCID.Mechanic => ProjectileID.MechanicWrench,
        NPCID.Painter => ProjectileID.PainterPaintball,
        NPCID.Wizard => ProjectileID.WaterBolt,
        NPCID.Truffle => ProjectileID.Mushroom,
        _ => NPCID.Sets.AttackType[type] switch
        {
            0 => ProjectileID.ThrowingKnife,
            1 => ProjectileID.Bullet,
            2 => ProjectileID.WaterBolt,
            _ => ProjectileID.WoodenArrowFriendly
        }
    };
}
