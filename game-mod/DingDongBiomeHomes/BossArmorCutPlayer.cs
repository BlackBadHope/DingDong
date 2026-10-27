using System;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

// Owner report (28.10): a boss melee hit strips almost all armor for the rest of the
// fight (defense-cutting debuffs from the pack: vanilla Broken Armor halves defense,
// Eternity-style "defenseless" states zero it). The pack keeps the debuffs, but 85 %
// of the defense they remove is given back at hurt time. The uncut value is observed
// right after vanilla equips are applied; whatever later mod hooks or the vanilla
// Broken Armor halving take away is restored through negative armor penetration,
// which the native hurt formula treats as extra defense.
public sealed class BossArmorCutPlayer : ModPlayer
{
	internal const float RestoreShare = 0.85f;
	private int defenseAfterEquips;
	private bool snapshotValid;

	public override void ResetEffects() { snapshotValid = false; }

	public override void PostUpdateEquips()
	{
		defenseAfterEquips = Player.statDefense;
		snapshotValid = true;
	}

	internal static float RestoredDefense(int afterEquips, int now, bool brokenArmor)
	{
		if (now < 0) now = 0;
		// Broken Armor halves defense; if the snapshot does not show that halving yet
		// (snapshot ~ current), the halving happens later and the uncut value is double.
		int original = brokenArmor && afterEquips < now * 1.5f ? afterEquips * 2 : afterEquips;
		int missing = original - now;
		return missing > 0 ? missing * RestoreShare : 0f;
	}

	public override void ModifyHurt(ref Player.HurtModifiers modifiers)
	{
		if (!snapshotValid) return;
		float restored = RestoredDefense(defenseAfterEquips, Player.statDefense, Player.brokenArmor);
		if (restored > 0f) modifiers.ArmorPenetration += -restored;
	}
}
