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
public sealed partial class MobRelicPlayer
{
	internal void Equip(MobRelicEffectProfile profile)
	{
		MobRelicMechanics.ApplyPrimary(Player, profile);
		activeProfiles[profile.MechanicsKey] = profile;
		Player.GetModPlayer<SynergyArmorPlayer>().ObserveRelic(profile);
		if (!equippedSets.TryGetValue(profile.Source.ContentKey, out MobRelicEquippedSet set))
		{
			set = new MobRelicEquippedSet();
			equippedSets.Add(profile.Source.ContentKey, set);
		}

		if (set.IsEmpty)
			tickSets.Add(set); // PerfOpt 28.10
		set.Register(profile);
		if (!providerSources.TryGetValue(profile.Source.Provider, out HashSet<string> sources))
		{
			sources = new HashSet<string>(StringComparer.Ordinal);
			providerSources.Add(profile.Source.Provider, sources);
		}

		if (sources.Count == 0)
			tickProviders.Add(sources); // PerfOpt 28.10
		sources.Add(profile.Source.ContentKey);
	}

	private void TryRunPersonalMechanics(NPC target, NPC.HitInfo hit, int damageDone, int damageMask)
	{
		if (damageDone <= 0 || target == null || !target.active || target.friendly || Player.whoAmI != Main.myPlayer)
		{
			return;
		}

		foreach (MobRelicEffectProfile profile in activeProfiles.Values)
		{
			if (!scriptStates.TryGetValue(profile.MechanicsKey, out MobRelicScriptState state))
			{
				state = new MobRelicScriptState();
				scriptStates.Add(profile.MechanicsKey, state);
			}

			bool triggered = TriggerMatches(profile, state, target, hit, damageDone, damageMask);
			if (triggered && state.Cooldown <= 0)
				InvokePersonalMechanic(profile, state, target, damageDone, damageMask);
		}
	}
}
