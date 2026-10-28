using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using DingDongBiomeHomes.Items.SynergyArmor;

namespace DingDongBiomeHomes.Items.MobRelics;
public sealed partial class MobRelicPlayer : ModPlayer
{
	private readonly Dictionary<string, MobRelicEquippedSet> equippedSets = new(StringComparer.Ordinal);
	private readonly Dictionary<string, HashSet<string>> providerSources = new(StringComparer.Ordinal);
	// PerfOpt 28.10: containers touched this tick, in first-touch (accessory slot) order, matching the old per-tick dictionary order.
	private readonly List<MobRelicEquippedSet> tickSets = new();
	private readonly List<HashSet<string>> tickProviders = new();
	private readonly Dictionary<string, MobRelicEffectProfile> activeProfiles = new(StringComparer.Ordinal);
	private readonly Dictionary<string, MobRelicScriptState> scriptStates = new(StringComparer.Ordinal);
	private MobRelicEffectProfile echoAnchor;
	private MobRelicEffectProfile wardAnchor;
	private int echoSetSize;
	private int wardSetSize;
	private bool hasEcho;
	private bool hasWard;
	private bool fullConvergence;
	private int echoCooldown;
	private int wardCooldown;
	private int wardActiveTicks;
	private int afterHurtTicks;
	internal int StrongestSetSize => Math.Max(echoSetSize, wardSetSize);
	internal bool EchoEnabled => hasEcho;
	internal bool WardEnabled => hasWard;
	internal bool FullConvergenceEnabled => fullConvergence;
	internal int WardActiveTicks => wardActiveTicks;
}
