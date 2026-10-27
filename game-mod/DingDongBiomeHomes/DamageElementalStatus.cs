using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

/// <summary>
/// Real elemental on-hit consequences for the existing persisted element tags:
/// fire ignites, frost chills, poison poisons, shadow burns with shadowflame,
/// water soaks and gel oils. Vanilla debuff IDs only, so NPC immunity data,
/// Oiled/fire synergy and net sync of NPC buffs stay native. Elements without
/// a safe vanilla consequence (lightning, earth, holy, astral, void, nature,
/// mod) intentionally map to nothing here instead of inventing side systems.
/// </summary>
public static class DamageElementalStatusPolicy
{
	public readonly record struct ElementalStatus(int BuffId, int DurationTicks, int MaxRemainingTicks);

	// Durations are short and readable; MaxRemaining caps repeated application
	// so sustained multihit weapons cannot pile minutes of damage-over-time.
	private static readonly Dictionary<string, ElementalStatus> Statuses = new(StringComparer.Ordinal)
	{
		[DamageTags.Fire] = new ElementalStatus(BuffID.OnFire, 240, 480),
		[DamageTags.Frost] = new ElementalStatus(BuffID.Frostburn, 240, 480),
		[DamageTags.Poison] = new ElementalStatus(BuffID.Poisoned, 300, 600),
		[DamageTags.Shadow] = new ElementalStatus(BuffID.ShadowFlame, 240, 480),
		[DamageTags.Water] = new ElementalStatus(BuffID.Wet, 300, 600),
		[DamageTags.Gel] = new ElementalStatus(BuffID.Oiled, 300, 600)
	};

	/// <summary>Chill marker reuses the vanilla Chilled debuff id; the slow
	/// itself is applied by <see cref="DamageElementalStatusGlobalNPC"/>.</summary>
	public const int ChillBuffId = BuffID.Chilled;
	public const int ChillDurationTicks = 150;
	public const int ChillMaxRemainingTicks = 180;
	public const int ChillResistTicks = 300;
	public const float ChillVelocityFactor = 0.82f;

	public static bool TryGetStatus(string elementTag, out ElementalStatus status) =>
		Statuses.TryGetValue(elementTag ?? string.Empty, out status);

	/// <summary>Targets that must never receive combat status effects.</summary>
	public static bool IsStatusExempt(NPC npc) =>
		npc is null || !npc.active || npc.friendly || npc.townNPC || npc.immortal || npc.dontTakeDamage;

	/// <summary>Control effects (the chill slow) stay off bosses, boss-counted
	/// encounters and multi-segment bodies; damage-over-time remains allowed.</summary>
	public static bool IsControlExempt(NPC npc) =>
		npc.boss || npc.realLife >= 0 || (npc.type >= 0 && npc.type < NPCID.Sets.ShouldBeCountedAsBoss.Length && NPCID.Sets.ShouldBeCountedAsBoss[npc.type]);

	/// <summary>
	/// Adds <paramref name="buffId"/> so the remaining time never exceeds
	/// <paramref name="maxRemainingTicks"/>. Vanilla AddBuff keeps the larger
	/// remaining time and already honors NPC buff immunity.
	/// </summary>
	public static void AddCappedBuff(NPC npc, int buffId, int durationTicks, int maxRemainingTicks)
	{
		int index = npc.FindBuffIndex(buffId);
		int remaining = index >= 0 ? npc.buffTime[index] : 0;
		int target = Math.Min(maxRemainingTicks, remaining + durationTicks);
		if (target > remaining)
			npc.AddBuff(buffId, target);
	}

	/// <summary>
	/// Night's Edge carries the biomes of its four source blades: hellfire,
	/// jungle venom and evil-biome shadowflame land together on every strike.
	/// Each element goes through the ordinary caps and exemptions above.
	/// </summary>
	public static void ApplyNightsEdgeBiomes(NPC npc)
	{
		ApplyElement(npc, DamageTags.Shadow);
		ApplyElement(npc, DamageTags.Fire);
		ApplyElement(npc, DamageTags.Poison);
	}

	public static void ApplyElement(NPC npc, string elementTag)
	{
		if (IsStatusExempt(npc))
			return;
		if (TryGetStatus(elementTag, out ElementalStatus status))
			AddCappedBuff(npc, status.BuffId, status.DurationTicks, status.MaxRemainingTicks);
		if (string.Equals(elementTag, DamageTags.Frost, StringComparison.Ordinal) && !IsControlExempt(npc)
			&& npc.TryGetGlobalNPC(out DamageElementalStatusGlobalNPC state) && state.ChillResistTicks <= 0)
		{
			AddCappedBuff(npc, ChillBuffId, ChillDurationTicks, ChillMaxRemainingTicks);
		}
	}
}

/// <summary>
/// Applies element statuses on every strike path that carries the persisted
/// element tags (melee items directly; projectiles, minions and sentries via
/// <see cref="DamageLineageGlobalProjectile"/>), and turns the vanilla Chilled
/// marker into a readable movement slow with an explicit anti-permastun gate:
/// after a full chill the target shrugs off further chills for a short while.
/// </summary>
public sealed class DamageElementalStatusGlobalNPC : GlobalNPC
{
	public override bool InstancePerEntity => true;

	/// <summary>Ticks this NPC refuses new chill applications.</summary>
	public int ChillResistTicks { get; internal set; }

	private int chillHeldTicks;

	public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
	{
		if (item.type == ItemID.NightsEdge)
			DamageElementalStatusPolicy.ApplyNightsEdgeBiomes(npc);
		else if (item.TryGetGlobalItem(out DamageAffinityGlobalItem affinity))
			DamageElementalStatusPolicy.ApplyElement(npc, affinity.ElementTag);
	}

	public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
	{
		if (!projectile.TryGetGlobalProjectile(out DamageLineageGlobalProjectile lineage))
			return;
		if (lineage.OriginItemType == ItemID.NightsEdge)
			DamageElementalStatusPolicy.ApplyNightsEdgeBiomes(npc);
		else
			DamageElementalStatusPolicy.ApplyElement(npc, lineage.ElementTag);
	}

	public override void PostAI(NPC npc)
	{
		// NPC buffs and AI are server-authoritative; remote clients receive the
		// synced buff array and velocity, so mutating either there would desync.
		if (Main.netMode == NetmodeID.MultiplayerClient)
			return;
		if (ChillResistTicks > 0)
			ChillResistTicks--;
		if (DamageElementalStatusPolicy.IsStatusExempt(npc) || DamageElementalStatusPolicy.IsControlExempt(npc))
		{
			chillHeldTicks = 0;
			return;
		}
		int index = npc.FindBuffIndex(DamageElementalStatusPolicy.ChillBuffId);
		if (index < 0)
		{
			chillHeldTicks = 0;
			return;
		}
		// A remote client cannot see ChillResistTicks, so its AddBuff packet may
		// still land during the resist window; the server strips it here.
		if (ChillResistTicks > 0)
		{
			npc.DelBuff(index);
			chillHeldTicks = 0;
			return;
		}
		npc.velocity *= DamageElementalStatusPolicy.ChillVelocityFactor;
		chillHeldTicks++;
		if (chillHeldTicks >= DamageElementalStatusPolicy.ChillMaxRemainingTicks)
		{
			npc.DelBuff(index);
			chillHeldTicks = 0;
			ChillResistTicks = DamageElementalStatusPolicy.ChillResistTicks;
		}
	}
}
