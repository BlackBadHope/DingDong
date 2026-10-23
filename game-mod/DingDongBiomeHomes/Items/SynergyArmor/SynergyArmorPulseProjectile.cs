using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.SynergyArmor;
public sealed partial class SynergyArmorPulseProjectile : ModProjectile
{
	public override string Texture => "DingDongBiomeHomes/Items/MobRelics/MobRelicResonanceProjectile";
	private SynergyArmorPulseMode Mode => (SynergyArmorPulseMode)(int)Projectile.ai[0];
}
