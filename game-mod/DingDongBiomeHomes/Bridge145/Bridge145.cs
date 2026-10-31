using System;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Utilities;

namespace DingDongBiomeHomes.Bridge145;

// Owner, 31.10: "bring Terraria 1.4.5 into the pack". tModLoader 1.4.4 reads vanilla art from the
// player's own Steam install of Terraria, which is 1.4.5 for every up-to-date player. The bridge
// therefore ships no Re-Logic art or code: each 1.4.5 creature is our own implementation whose
// texture is requested by its vanilla path. The creature is registered on every machine (a dedicated
// server loads no art, so a file test there would disagree with clients and desync NPC types); a
// client whose install lacks the 1.4.5 file draws a vanilla fallback sprite instead. The bridge
// switches itself off once tModLoader knows the creature natively (its NPCID is below Count).
internal static class Bridge145
{
    internal static bool Has(string vanillaImage)
    {
        try { return ModContent.HasAsset("Terraria/Images/" + vanillaImage); }
        catch (Exception) { return false; }
    }
}

// Orca (1.4.5 NPC 692): a heavier shark of the hardmode ocean. Facts from the wiki: swimming AI,
// 400 life, 50 damage, 20 defense, 30% knockback resistance, immune to Confused; drops a Diving
// Helmet (5%) or else a Shark Fin, Shrimp Po' Boy 2%. Sprite: 16 frames, 120x34 hitbox.
public sealed class Orca145 : ModNPC
{
    private const int VanillaId = 692;
    private static bool Native => !Main.dedServ && Bridge145.Has("NPC_" + VanillaId);
    // Fallback without 1.4.5: the vanilla Shark (4 frames).
    private static int Frames => Native ? 16 : 4;
    public override string Texture => Native ? "Terraria/Images/NPC_" + VanillaId : "Terraria/Images/NPC_" + NPCID.Shark;
    public override bool IsLoadingEnabled(Mod mod) => NPCID.Count <= VanillaId;

    public override void SetStaticDefaults()
    {
        Main.npcFrameCount[Type] = Frames;
        NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
    }

    public override void SetDefaults()
    {
        NPC.width = 120; NPC.height = 34;
        NPC.aiStyle = NPCAIStyleID.Piranha; AIType = NPCID.Shark;
        NPC.damage = 50; NPC.defense = 20; NPC.lifeMax = 400; NPC.knockBackResist = .7f;
        NPC.noGravity = true; NPC.value = Item.buyPrice(gold: 2, silver: 50);
        NPC.HitSound = SoundID.NPCHit1; NPC.DeathSound = SoundID.NPCDeath1;
    }

    public override float SpawnChance(NPCSpawnInfo info) =>
        Main.hardMode && info.Water && !info.PlayerInTown && info.Player.ZoneBeach ? SpawnCondition.OceanMonster.Chance * .35f : 0f;

    public override void FindFrame(int frameHeight)
    {
        NPC.spriteDirection = NPC.direction;
        if (++NPC.frameCounter >= 5) { NPC.frameCounter = 0; NPC.frame.Y = (NPC.frame.Y + frameHeight) % (frameHeight * Frames); }
    }

    public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) =>
        bestiaryEntry.Info.Add(BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Ocean);

    public override void ModifyNPCLoot(NPCLoot npcLoot)
    {
        // Plain Common rules: the hard-loot coverage gate refuses rule types it does not model
        // (S35 x10: SequentialRulesNotScalingWithLuck). Shark Fin always, Diving Helmet 1 in 20.
        npcLoot.Add(ItemDropRule.Common(ItemID.SharkFin));
        npcLoot.Add(ItemDropRule.Common(ItemID.DivingHelmet, 20));
        npcLoot.Add(ItemDropRule.Common(ItemID.ShrimpPoBoy, 50));
    }
}

// One line per load so a server log shows what the bridge enabled (client and server must agree).
public sealed class Bridge145Report : ModSystem
{
    public override void PostSetupContent() =>
        Mod.Logger.Info($"DINGDONG_BRIDGE145|orca={(ModContent.TryFind<ModNPC>(Mod.Name, nameof(Orca145), out _) ? "loaded" : "off")}|orcaAsset={Bridge145.Has("NPC_692")}|vanillaNpcCount={NPCID.Count}|server={Main.dedServ}");
}
