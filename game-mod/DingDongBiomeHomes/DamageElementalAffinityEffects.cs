using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

/// <summary>
/// On-hit consequences for the elements that have no fitting vanilla NPC
/// debuff. Each is a plain NPC debuff (vanilla icon, no new art), applied by
/// <see cref="DamageElementalStatusPolicy"/> through the same capped AddBuff
/// path as fire or poison, so buff immunity, timers and multiplayer sync are
/// the native NPC buff sync. The flags below are derived every tick from the
/// synced buff array, hence identical on the server and on every client.
///
/// Balance (vanilla reference: On Fire 4 hp/s, Poisoned 6, Frostburn 8,
/// Shadowflame 15):
///  lightning  Static Shock   5 hp/s, 15 hp/s on wet targets (water/rain synergy)
///  earth      Cracked Armor  hits against the target ignore 8 defense
///  holy       Radiance       4 hp/s, its contact hits deal 8% less damage
///  astral     Star Mark      takes 6% more damage from every hit
///  void       Void Drain     6 hp/s
/// None of them is a control effect, so bosses are not exempt; the usual
/// status exemptions (town NPCs, immortal, dontTakeDamage) still apply.
/// </summary>
public static class DamageElementalAffinityTuning
{
	public const int StaticShockRegenLoss = 10;
	public const int StaticShockWetRegenLoss = 30;
	public const int CrackedArmorPenetration = 8;
	public const int RadianceRegenLoss = 8;
	public const float RadianceContactDamageFactor = 0.92f;
	public const float StarMarkDamageFactor = 1.06f;
	public const int VoidDrainRegenLoss = 12;
}

public abstract class DamageElementalDebuff : ModBuff
{
	protected abstract int VanillaIcon { get; }

	public override string Texture => "Terraria/Images/Buff_" + VanillaIcon;

	public override void SetStaticDefaults()
	{
		Main.debuff[Type] = true;
		Main.buffNoSave[Type] = true;
	}

	public override void Update(NPC npc, ref int buffIndex)
	{
		if (npc.TryGetGlobalNPC(out DamageElementalAffinityGlobalNPC state))
			Mark(state);
	}

	protected abstract void Mark(DamageElementalAffinityGlobalNPC state);
}

public sealed class StaticShockDebuff : DamageElementalDebuff
{
	protected override int VanillaIcon => BuffID.Electrified;
	protected override void Mark(DamageElementalAffinityGlobalNPC state) => state.StaticShock = true;
}

public sealed class CrackedArmorDebuff : DamageElementalDebuff
{
	protected override int VanillaIcon => BuffID.BrokenArmor;
	protected override void Mark(DamageElementalAffinityGlobalNPC state) => state.CrackedArmor = true;
}

public sealed class RadianceDebuff : DamageElementalDebuff
{
	protected override int VanillaIcon => BuffID.Shine;
	protected override void Mark(DamageElementalAffinityGlobalNPC state) => state.Radiance = true;
}

public sealed class StarMarkDebuff : DamageElementalDebuff
{
	protected override int VanillaIcon => BuffID.StarInBottle;
	protected override void Mark(DamageElementalAffinityGlobalNPC state) => state.StarMark = true;
}

public sealed class VoidDrainDebuff : DamageElementalDebuff
{
	protected override int VanillaIcon => BuffID.VortexDebuff;
	protected override void Mark(DamageElementalAffinityGlobalNPC state) => state.VoidDrain = true;
}

public sealed class DamageElementalAffinityGlobalNPC : GlobalNPC
{
	public override bool InstancePerEntity => true;

	public bool StaticShock { get; internal set; }
	public bool CrackedArmor { get; internal set; }
	public bool Radiance { get; internal set; }
	public bool StarMark { get; internal set; }
	public bool VoidDrain { get; internal set; }

	public override void ResetEffects(NPC npc)
	{
		StaticShock = false;
		CrackedArmor = false;
		Radiance = false;
		StarMark = false;
		VoidDrain = false;
	}

	public static bool IsWet(NPC npc) => npc.wet || npc.HasBuff(BuffID.Wet);

	public override void UpdateLifeRegen(NPC npc, ref int damage)
	{
		if (StaticShock)
		{
			bool wet = IsWet(npc);
			Drain(npc, ref damage, wet ? DamageElementalAffinityTuning.StaticShockWetRegenLoss : DamageElementalAffinityTuning.StaticShockRegenLoss, wet ? 5 : 2);
		}
		if (Radiance)
			Drain(npc, ref damage, DamageElementalAffinityTuning.RadianceRegenLoss, 2);
		if (VoidDrain)
			Drain(npc, ref damage, DamageElementalAffinityTuning.VoidDrainRegenLoss, 3);
	}

	private static void Drain(NPC npc, ref int damage, int regenLoss, int shownDamage)
	{
		if (npc.lifeRegen > 0)
			npc.lifeRegen = 0;
		npc.lifeRegen -= regenLoss;
		if (damage < shownDamage)
			damage = shownDamage;
	}

	public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
	{
		if (CrackedArmor)
			modifiers.ArmorPenetration += DamageElementalAffinityTuning.CrackedArmorPenetration;
		if (StarMark)
			modifiers.FinalDamage *= DamageElementalAffinityTuning.StarMarkDamageFactor;
	}

	public override void ModifyHitPlayer(NPC npc, Player target, ref Player.HurtModifiers modifiers)
	{
		if (Radiance)
			modifiers.SourceDamage *= DamageElementalAffinityTuning.RadianceContactDamageFactor;
	}

	public override void DrawEffects(NPC npc, ref Color drawColor)
	{
		// Cosmetic only (clients); pooled dust, no allocation.
		if (StaticShock && Main.rand.NextBool(5))
			Sparkle(npc, DustID.Electric);
		if (CrackedArmor && Main.rand.NextBool(8))
			Sparkle(npc, DustID.Stone);
		if (Radiance)
		{
			Lighting.AddLight(npc.Center, 0.45f, 0.4f, 0.2f);
			if (Main.rand.NextBool(6))
				Sparkle(npc, DustID.GoldFlame);
		}
		if (StarMark && Main.rand.NextBool(6))
			Sparkle(npc, DustID.MagicMirror);
		if (VoidDrain && Main.rand.NextBool(6))
			Sparkle(npc, DustID.PurpleTorch);
	}

	private static void Sparkle(NPC npc, int dustType)
	{
		Dust dust = Dust.NewDustDirect(npc.position, npc.width, npc.height, dustType, 0f, -0.5f, 100, default, 0.9f);
		dust.noGravity = true;
	}
}
