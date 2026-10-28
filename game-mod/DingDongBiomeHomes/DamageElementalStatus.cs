using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

/// <summary>
/// Real elemental on-hit consequences for the existing persisted element tags:
/// fire ignites, frost chills, poison poisons, shadow burns with shadowflame,
/// water soaks, gel oils and nature calls the vanilla Dryad's Bane. Elements
/// without a fitting vanilla NPC debuff (lightning, earth, holy, astral, void)
/// use small DingDong debuffs (see DamageElementalAffinityEffects.cs) that are
/// ordinary NPC buffs too, so immunity, timers and net sync stay native.
/// Neutral and the foreign "mod element" adapter label apply nothing.
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
		[DamageTags.Gel] = new ElementalStatus(BuffID.Oiled, 300, 600),
		[DamageTags.Nature] = new ElementalStatus(BuffID.DryadsWardDebuff, 240, 480)
	};

	/// <summary>A refresh that would add less than this many ticks is skipped:
	/// sustained multihit (and composite weapons applying several elements)
	/// then sends a buff packet only when the timer meaningfully changes.</summary>
	public const int MinRefreshGainTicks = 30;

	/// <summary>Chill marker reuses the vanilla Chilled debuff id; the slow
	/// itself is applied by <see cref="DamageElementalStatusGlobalNPC"/>.</summary>
	public const int ChillBuffId = BuffID.Chilled;
	public const int ChillDurationTicks = 150;
	public const int ChillMaxRemainingTicks = 180;
	public const int ChillResistTicks = 300;
	public const float ChillVelocityFactor = 0.82f;

	public static bool TryGetStatus(string elementTag, out ElementalStatus status)
	{
		if (Statuses.TryGetValue(elementTag ?? string.Empty, out status))
			return true;
		// Mod buff ids exist only after load, so they are resolved here (a
		// static generic lookup, no allocation) instead of in the table.
		status = elementTag switch
		{
			DamageTags.Lightning => new ElementalStatus(ModContent.BuffType<StaticShockDebuff>(), 180, 360),
			DamageTags.Earth => new ElementalStatus(ModContent.BuffType<CrackedArmorDebuff>(), 300, 600),
			DamageTags.Holy => new ElementalStatus(ModContent.BuffType<RadianceDebuff>(), 240, 480),
			DamageTags.Astral => new ElementalStatus(ModContent.BuffType<StarMarkDebuff>(), 240, 480),
			DamageTags.Void => new ElementalStatus(ModContent.BuffType<VoidDrainDebuff>(), 240, 480),
			_ => default
		};
		return status.BuffId > 0;
	}

	/// <summary>Localization key suffix of an element's on-hit effect text.</summary>
	public static string EffectKey(string elementTag) => elementTag switch
	{
		DamageTags.Fire => "Fire",
		DamageTags.Frost => "Frost",
		DamageTags.Poison => "Poison",
		DamageTags.Shadow => "Shadow",
		DamageTags.Water => "Water",
		DamageTags.Gel => "Gel",
		DamageTags.Nature => "Nature",
		DamageTags.Lightning => "Lightning",
		DamageTags.Earth => "Earth",
		DamageTags.Holy => "Holy",
		DamageTags.Astral => "Astral",
		DamageTags.Void => "Void",
		_ => "None"
	};

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
		if (target > remaining && (remaining <= 0 || target - remaining >= MinRefreshGainTicks))
			npc.AddBuff(buffId, target);
	}

	/// <summary>
	/// Night's Edge carries the biomes of its four source blades: hellfire,
	/// jungle venom and evil-biome shadowflame land together on every strike.
	/// Each element goes through the ordinary caps and exemptions above.
	/// </summary>
	public static void ApplyNightsEdgeBiomes(NPC npc) => ApplyItemElements(npc, ItemID.NightsEdge, DamageTags.Shadow);

	/// <summary>
	/// The struck weapon's own element plus, for a composite weapon, the
	/// biome element of each source weapon (<see cref="CompositeWeaponLineage"/>),
	/// at most <see cref="CompositeWeaponLineage.MaxInheritedElements"/> statuses
	/// per hit. Each status keeps its own caps and exemptions.
	/// </summary>
	public static void ApplyItemElements(NPC npc, int originItemType, string ownElement)
	{
		if (IsStatusExempt(npc))
			return;
		ApplyElement(npc, ownElement);
		int applied = TryGetStatus(ownElement, out _) ? 1 : 0;
		CompositeWeaponLineage.InheritedElement[] inherited = CompositeWeaponLineage.Get(originItemType);
		for (int i = 0; i < inherited.Length && applied < CompositeWeaponLineage.MaxInheritedElements; i++)
		{
			if (string.Equals(inherited[i].Element, ownElement, StringComparison.Ordinal))
				continue;
			ApplyElement(npc, inherited[i].Element);
			applied++;
		}
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
		string own = item.TryGetGlobalItem(out DamageAffinityGlobalItem affinity) ? affinity.ElementTag : DamageTags.Neutral;
		DamageElementalStatusPolicy.ApplyItemElements(npc, item.type, own);
	}

	public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
	{
		if (!projectile.TryGetGlobalProjectile(out DamageLineageGlobalProjectile lineage))
			return;
		DamageElementalStatusPolicy.ApplyItemElements(npc, lineage.OriginItemType, lineage.ElementTag);
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
