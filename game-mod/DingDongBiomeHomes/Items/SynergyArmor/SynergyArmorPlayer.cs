using System;
using System.Collections.Generic;
using System.Linq;
using DingDongBiomeHomes.Items.MobRelics;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.SynergyArmor;

public sealed partial class SynergyArmorPlayer : ModPlayer
{
	private readonly int[] aspectCounts = new int[Enum.GetValues<MobRelicAspect>().Length];
	private readonly int[] tierCounts = new int[Enum.GetValues<MobRelicTier>().Length];
	private readonly HashSet<string> providers = new(StringComparer.Ordinal);
	private readonly HashSet<string> relicSources = new(StringComparer.Ordinal);
	private SynergyArmorSetDefinition activeSet;
	private Vector2 previousCenter;
	private float travelCharge;
	private float stormCharge;
	private int stillnessTicks;
	private int primaryCounter;
	private int trackedTarget = -1;
	private int classMask;
	private int cooldown;
	private int temporaryWardTicks;
	private int storedDamage;
	private int delayedTimer;
	private int delayedTarget = -1;
	private int commissionTicks;
	private bool relicEchoEnabled;
	private bool relicWardEnabled;
	private bool relicConvergenceEnabled;

	internal SynergyArmorSetId? ActiveSetId => activeSet?.Id;
	internal int ProviderCount => providers.Count;
	internal int RelicSourceCount => relicSources.Count;
	internal int ActiveAspectCount => aspectCounts.Count(value => value > 0);
	internal int ActiveTierCount => tierCounts.Count(value => value > 0);

	public override void ResetEffects()
	{
		activeSet = null;
		Array.Clear(aspectCounts);
		Array.Clear(tierCounts);
		providers.Clear();
		relicSources.Clear();
		relicEchoEnabled = false;
		relicWardEnabled = false;
		relicConvergenceEnabled = false;
	}

	public override void UpdateDead()
	{
		ResetTransientState();
		cooldown = 0;
		commissionTicks = 0;
	}

	internal void Activate(SynergyArmorSetDefinition definition) => activeSet = definition;

	internal void ObserveRelic(MobRelicEffectProfile profile)
	{
		aspectCounts[(int)profile.Aspect]++;
		tierCounts[(int)profile.Tier]++;
		providers.Add(profile.Source.Provider);
		relicSources.Add(profile.Source.ContentKey);
	}

	private bool HasAspect(MobRelicAspect aspect) => aspectCounts[(int)aspect] > 0;

	private void ResetTransientState()
	{
		travelCharge = 0f;
		stormCharge = 0f;
		stillnessTicks = 0;
		primaryCounter = 0;
		trackedTarget = -1;
		classMask = 0;
		temporaryWardTicks = 0;
		storedDamage = 0;
		delayedTimer = 0;
		delayedTarget = -1;
	}
}
