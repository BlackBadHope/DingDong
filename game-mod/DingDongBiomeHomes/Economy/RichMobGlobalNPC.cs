using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes.Economy;

// Owner, 30.10 (money rework): an ordinary monster has no reason to carry coins, so killed
// monsters drop none and a boss pays only through its treasure bag (vanilla bags read the
// boss's catalogue value, which stays intact). Monsters that plausibly carry money may spawn
// "rich": zombies, skeletons (they were alive once), goblins, pirates, anything in armour,
// golden monsters and mimics (always rich). Only a rich one drops coins when it dies.
public sealed class RichMobGlobalNPC : GlobalNPC
{
	internal enum Purse : byte { None, Zombie, Skeleton, Goblin, Armored, Pirate, Golden, Mimic, Generic }

	internal bool Rich;
	private Purse purse;
	private int catalogueValue;

	public override bool InstancePerEntity => true;

	public override bool AppliesToEntity(NPC entity, bool lateInstantiation) =>
		lateInstantiation && !entity.townNPC && !entity.friendly && entity.lifeMax > 5 && !NPCID.Sets.CountsAsCritter[entity.type];

	public override void OnSpawn(NPC npc, IEntitySource source)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient) return;
		catalogueValue = (int)Math.Min(int.MaxValue, npc.value);
		// No monster drops coins by itself any more; bosses pay through their bags.
		npc.value = 0;
		if (npc.boss || NPCID.Sets.ShouldBeCountedAsBoss[npc.type] || npc.SpawnedFromStatue) return;
		purse = Classify(npc);
		Rich = purse is Purse.Golden or Purse.Mimic || (purse != Purse.None && Main.rand.NextFloat() < RichChance);
		if (Rich) npc.netUpdate = true;
	}

	internal const float RichChance = .18f;

	internal static Purse Classify(NPC npc)
	{
		int t = npc.type;
		if (t is NPCID.Mimic or NPCID.IceMimic or NPCID.PresentMimic or NPCID.BigMimicCorruption or NPCID.BigMimicCrimson or NPCID.BigMimicHallow or NPCID.BigMimicJungle) return Purse.Mimic;
		if (t is NPCID.GoldenSlime) return Purse.Golden;
		if (NPCID.Sets.Zombies[t]) return Purse.Zombie;
		if (NPCID.Sets.Skeletons[t]) return Purse.Skeleton;
		if (t is NPCID.GoblinPeon or NPCID.GoblinThief or NPCID.GoblinWarrior or NPCID.GoblinSorcerer or NPCID.GoblinArcher or NPCID.GoblinScout or NPCID.GoblinSummoner) return Purse.Goblin;
		if (t is NPCID.PirateDeckhand or NPCID.PirateCorsair or NPCID.PirateDeadeye or NPCID.PirateCrossbower or NPCID.PirateCaptain or NPCID.PirateGhost) return Purse.Pirate;
		if (t is NPCID.ArmoredSkeleton or NPCID.ArmoredViking or NPCID.PossessedArmor or NPCID.Paladin or NPCID.BlueArmoredBones or NPCID.RustyArmoredBonesAxe or NPCID.HellArmoredBones or NPCID.UndeadMiner or NPCID.Tim or NPCID.DarkCaster) return Purse.Armored;
		string name = npc.ModNPC?.Name ?? NPCID.Search.GetName(t);
		if (name == null) return Purse.None;
		if (name.Contains("Mimic")) return Purse.Mimic;
		if (name.Contains("Golden") || name.StartsWith("Gold")) return Purse.Golden;
		if (name.Contains("Zombie")) return Purse.Zombie;
		if (name.Contains("Skeleton") || name.Contains("Bones")) return Purse.Skeleton;
		if (name.Contains("Goblin")) return Purse.Goblin;
		if (name.Contains("Pirate") || name.Contains("Bandit") || name.Contains("Thief")) return Purse.Pirate;
		if (name.Contains("Armor") || name.Contains("Armour") || name.Contains("Knight") || name.Contains("Soldier") || name.Contains("Guard") || name.Contains("Viking") || name.Contains("Paladin")) return Purse.Armored;
		return Purse.None;
	}

	/// <summary>Copper range a rich monster of this kind carries (owner: zombie 1 copper..59 silver, goblin 1 copper..25 silver).</summary>
	internal static (int Min, int Max) Range(Purse purse) => purse switch
	{
		Purse.Zombie => (1, 5_900),
		Purse.Goblin => (1, 2_500),
		Purse.Skeleton => (1, 5_000),
		Purse.Armored => (100, 10_000),
		Purse.Pirate => (500, 20_000),
		Purse.Golden or Purse.Mimic => (1_000, 50_000),
		_ => (1, 2_000)
	};

	public override void OnKill(NPC npc)
	{
		if (!Rich || Main.netMode == NetmodeID.MultiplayerClient) return;
		(int min, int max) = Range(purse);
		long copper = Main.rand.Next(min, max + 1);
		if (purse is Purse.Golden or Purse.Mimic) copper = Math.Max(copper, catalogueValue);
		if (Main.hardMode) copper *= 2;
		DropCoins(npc, copper);
	}

	internal static void DropCoins(NPC npc, long copper)
	{
		var source = npc.GetSource_Loot();
		foreach ((int type, long unit) in new[] { (ItemID.PlatinumCoin, 1_000_000L), (ItemID.GoldCoin, 10_000L), (ItemID.SilverCoin, 100L), (ItemID.CopperCoin, 1L) })
		{
			int count = (int)(copper / unit);
			copper %= unit;
			while (count > 0)
			{
				int stack = Math.Min(count, 999);
				Item.NewItem(source, npc.getRect(), type, stack);
				count -= stack;
			}
		}
	}

	public override void ModifyTypeName(NPC npc, ref string typeName)
	{
		if (Rich) typeName = Language.GetTextValue("Mods.DingDongBiomeHomes.Economy.RichName", typeName);
	}

	public override void DrawEffects(NPC npc, ref Color drawColor)
	{
		if (!Rich || Main.rand.Next(24) != 0) return;
		Dust dust = Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.GoldCoin, 0f, -1f);
		dust.noGravity = true; dust.scale = .9f;
	}

	public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter) => bitWriter.WriteBit(Rich);
	public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader) => Rich = bitReader.ReadBit();
}
