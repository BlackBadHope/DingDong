using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Utilities;

namespace DingDongBiomeHomes.Npcs.MobVariety;

[Flags]
internal enum MobMutation : byte
{
	None = 0, Crippled = 1, Empowered = 2, Elemental = 4, Armored = 8, Swift = 16, Regenerating = 32, Mutant = 64
}

internal enum MobElement : byte { None, Fire, Frost, Poison, Shadow, Holy, Lightning, Water, Earth }

/// <summary>
/// v0.3 (owner, 29.10): the same kind of creature is no longer always the same. On spawn the
/// server may mark an ordinary hostile creature with one or two mutations (crippled, empowered,
/// elemental, armored, swift, regenerating, mutant); the mark changes its stats, name, tint and,
/// for elemental ones, the debuff it inflicts. The chance grows with world progression. Bosses,
/// boss minions, worm segments, critters, town NPCs and statue spawns are never marked. The mark
/// is rolled once on the server and travels with the native NPC sync (two bytes of extra AI);
/// clients apply the same stat change once when they first receive it.
/// </summary>
public sealed class MobVarietyGlobalNPC : GlobalNPC
{
	private const string Key = "Mods.DingDongBiomeHomes.MobVariety.";
	internal const float SecondMutationChance = 0.25f;
	internal const int RegenDelayTicks = 180, RegenIntervalTicks = 60;
	internal const float RegenShare = 0.02f;

	internal MobMutation Flags;
	internal MobElement Element;
	private bool applied;
	private uint lastHurt;

	public override bool InstancePerEntity => true;

	public override bool AppliesToEntity(NPC entity, bool lateInstantiation) => lateInstantiation && Eligible(entity);

	internal static bool Eligible(NPC npc) =>
		!npc.townNPC && !npc.friendly && !npc.boss && !npc.dontTakeDamage && npc.lifeMax > 5 && npc.damage > 0 &&
		npc.type >= 0 && npc.type < NPCLoader.NPCCount && !NPCID.Sets.CountsAsCritter[npc.type] &&
		!NPCID.Sets.ProjectileNPC[npc.type] && !NPCID.Sets.ShouldBeCountedAsBoss[npc.type] && npc.aiStyle != NPCAIStyleID.Worm;

	internal static float Chance() =>
		NPC.downedMoonlord ? 0.32f : NPC.downedPlantBoss ? 0.28f : Main.hardMode ? 0.22f : 0.16f;

	public override void OnSpawn(NPC npc, IEntitySource source)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient || npc.SpawnedFromStatue || npc.realLife >= 0) return;
		if (source is EntitySource_Parent { Entity: NPC parent } && (parent.boss || NPCID.Sets.ShouldBeCountedAsBoss[parent.type])) return;
		if (Main.rand.NextFloat() >= Chance()) return;
		Flags = Roll(Main.rand);
		Element = (Flags & MobMutation.Elemental) != 0 ? ElementAt(npc) : MobElement.None;
		Apply(npc);
		npc.netUpdate = true;
	}

	internal static MobMutation Roll(UnifiedRandom rand)
	{
		MobMutation first = Pick(rand, MobMutation.None);
		if (first == MobMutation.Mutant) return first | Pick(rand, MobMutation.Crippled | MobMutation.Mutant);
		if (rand.NextFloat() < SecondMutationChance)
		{
			MobMutation exclude = first | (first == MobMutation.Crippled ? MobMutation.Empowered | MobMutation.Mutant : MobMutation.Crippled) | MobMutation.Mutant;
			return first | Pick(rand, exclude);
		}
		return first;
	}

	private static readonly (MobMutation Flag, int Weight)[] Weights =
	{
		(MobMutation.Crippled, 18), (MobMutation.Empowered, 20), (MobMutation.Elemental, 20), (MobMutation.Armored, 14),
		(MobMutation.Swift, 12), (MobMutation.Regenerating, 8), (MobMutation.Mutant, 8)
	};

	private static MobMutation Pick(UnifiedRandom rand, MobMutation exclude)
	{
		int total = 0;
		foreach (var (flag, weight) in Weights) if ((exclude & flag) == 0) total += weight;
		int roll = rand.Next(total);
		foreach (var (flag, weight) in Weights)
		{
			if ((exclude & flag) != 0) continue;
			if (roll < weight) return flag;
			roll -= weight;
		}
		return MobMutation.Empowered;
	}

	private static MobElement ElementAt(NPC npc)
	{
		Player p = Main.player[Player.FindClosest(npc.position, npc.width, npc.height)];
		if (p.ZoneUnderworldHeight) return MobElement.Fire;
		if (p.ZoneSnow) return MobElement.Frost;
		if (p.ZoneJungle) return MobElement.Poison;
		if (p.ZoneCorrupt || p.ZoneCrimson) return MobElement.Shadow;
		if (p.ZoneHallow) return MobElement.Holy;
		if (p.ZoneBeach) return MobElement.Water;
		if (p.ZoneDesert || p.ZoneUndergroundDesert) return MobElement.Earth;
		if (p.ZoneSkyHeight) return MobElement.Lightning;
		return (MobElement)(1 + Main.rand.Next(8));
	}

	internal void Apply(NPC npc)
	{
		if (applied || Flags == MobMutation.None) return;
		applied = true;
		float hp = 1f, damage = 1f, value = 1f, scale = 1f;
		if (Has(MobMutation.Crippled)) { hp *= 0.7f; damage *= 0.75f; value *= 0.8f; scale *= 0.9f; }
		if (Has(MobMutation.Empowered)) { hp *= 1.4f; damage *= 1.2f; value *= 1.5f; }
		if (Has(MobMutation.Elemental)) { hp *= 1.1f; value *= 1.3f; }
		if (Has(MobMutation.Armored))
		{
			hp *= 1.1f; value *= 1.3f;
			npc.defense = npc.defDefense = npc.defDefense + npc.defDefense / 2 + 6;
			npc.knockBackResist *= 0.6f;
		}
		if (Has(MobMutation.Swift)) { damage *= 1.05f; value *= 1.2f; }
		if (Has(MobMutation.Regenerating)) value *= 1.25f;
		if (Has(MobMutation.Mutant)) { hp *= 1.6f; damage *= 1.25f; value *= 2f; scale *= 1.2f; }
		npc.lifeMax = Math.Max(1, (int)(npc.lifeMax * hp));
		// The server owns life; a client keeps the synced value and only clamps it to the new maximum.
		npc.life = Main.netMode == NetmodeID.MultiplayerClient ? Math.Clamp(npc.life, 1, npc.lifeMax) : npc.lifeMax;
		npc.damage = npc.defDamage = Math.Max(1, (int)(npc.defDamage * damage));
		npc.value *= value;
		npc.scale *= scale;
	}

	private bool Has(MobMutation flag) => (Flags & flag) != 0;

	internal float SpeedFactor() => Has(MobMutation.Swift) ? 1.3f : Has(MobMutation.Crippled) ? 0.8f : 1f;

	public override void PostAI(NPC npc)
	{
		if (Flags == MobMutation.None) return;
		float factor = SpeedFactor();
		if (factor != 1f && npc.velocity != Vector2.Zero)
		{
			Vector2 extra = npc.velocity * (factor - 1f);
			if (!npc.noTileCollide) extra = Collision.TileCollision(npc.position, extra, npc.width, npc.height, true, true);
			npc.position += extra;
		}
		if (Has(MobMutation.Regenerating) && Main.netMode != NetmodeID.MultiplayerClient && npc.life < npc.lifeMax &&
			Main.GameUpdateCount - lastHurt > RegenDelayTicks && Main.GameUpdateCount % RegenIntervalTicks == 0)
		{
			int heal = Math.Max(1, (int)(npc.lifeMax * RegenShare));
			npc.life = Math.Min(npc.lifeMax, npc.life + heal);
			npc.HealEffect(heal);
			npc.netUpdate = true;
		}
	}

	public override void HitEffect(NPC npc, NPC.HitInfo hit) => lastHurt = Main.GameUpdateCount;

	public override void OnHitPlayer(NPC npc, Player target, Player.HurtInfo hurtInfo)
	{
		if (!Has(MobMutation.Elemental)) return;
		(int buff, int time) = Element switch
		{
			MobElement.Fire => (BuffID.OnFire, 180),
			MobElement.Frost => (BuffID.Frostburn, 150),
			MobElement.Poison => (BuffID.Poisoned, 240),
			MobElement.Shadow => (BuffID.Darkness, 240),
			MobElement.Holy => (BuffID.Confused, 60),
			MobElement.Lightning => (BuffID.Electrified, 120),
			MobElement.Water => (BuffID.Slow, 120),
			MobElement.Earth => (BuffID.BrokenArmor, 180),
			_ => (0, 0)
		};
		if (buff > 0) target.AddBuff(buff, time);
	}

	public override void ModifyTypeName(NPC npc, ref string typeName)
	{
		if (Flags == MobMutation.None) return;
		var marks = new List<string>();
		foreach (var (flag, _) in Weights)
			if (Has(flag))
				marks.Add(flag == MobMutation.Elemental && Element != MobElement.None
					? Language.GetTextValue(Key + "ElementalOf", Language.GetTextValue(Key + "Element." + Element))
					: Language.GetTextValue(Key + "Flag." + flag));
		typeName = Language.GetTextValue(Key + "Name", typeName, string.Join(", ", marks));
	}

	public override void DrawEffects(NPC npc, ref Color drawColor)
	{
		if (Flags == MobMutation.None) return;
		drawColor = drawColor.MultiplyRGB(Color.Lerp(Color.White, Tint(), 0.45f));
		int dust = Has(MobMutation.Elemental) ? ElementDust() : Has(MobMutation.Mutant) ? DustID.CursedTorch : Has(MobMutation.Regenerating) ? DustID.PinkTorch : -1;
		if (dust >= 0 && Main.rand.NextBool(10))
		{
			Dust d = Dust.NewDustDirect(npc.position, npc.width, npc.height, dust, 0f, -1f, 100, default, 1.1f);
			d.noGravity = true;
		}
	}

	private Color Tint()
	{
		if (Has(MobMutation.Mutant)) return new Color(120, 255, 120);
		if (Has(MobMutation.Elemental)) return Element switch
		{
			MobElement.Fire => new Color(255, 140, 60), MobElement.Frost => new Color(150, 220, 255),
			MobElement.Poison => new Color(150, 230, 90), MobElement.Shadow => new Color(170, 110, 220),
			MobElement.Holy => new Color(255, 240, 160), MobElement.Lightning => new Color(120, 200, 255),
			MobElement.Water => new Color(90, 160, 255), MobElement.Earth => new Color(200, 160, 110),
			_ => Color.White
		};
		if (Has(MobMutation.Empowered)) return new Color(255, 110, 110);
		if (Has(MobMutation.Armored)) return new Color(170, 180, 200);
		if (Has(MobMutation.Swift)) return new Color(140, 210, 255);
		if (Has(MobMutation.Regenerating)) return new Color(255, 150, 200);
		return new Color(150, 140, 130); // crippled
	}

	private int ElementDust() => Element switch
	{
		MobElement.Fire => DustID.Torch, MobElement.Frost => DustID.IceTorch, MobElement.Poison => DustID.JungleTorch,
		MobElement.Shadow => DustID.Shadowflame, MobElement.Holy => DustID.HallowedTorch, MobElement.Lightning => DustID.Electric,
		MobElement.Water => DustID.Water, MobElement.Earth => DustID.Sand, _ => -1
	};

	public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
	{
		binaryWriter.Write((byte)Flags);
		binaryWriter.Write((byte)Element);
	}

	public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
	{
		var flags = (MobMutation)binaryReader.ReadByte();
		var element = (MobElement)binaryReader.ReadByte();
		if (applied || flags == MobMutation.None) return;
		Flags = flags;
		Element = element;
		Apply(npc);
	}
}
