using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.SynergyArmor;
public sealed partial class SynergyArmorPulseProjectile
{
	private static Color ModeColor(SynergyArmorPulseMode mode) => mode switch
	{
		SynergyArmorPulseMode.Briar => new Color(82, 196, 92),
		SynergyArmorPulseMode.Prism => new Color(245, 180, 255),
		SynergyArmorPulseMode.Burrow => new Color(196, 142, 78),
		SynergyArmorPulseMode.Hive => new Color(255, 208, 62),
		SynergyArmorPulseMode.Route => new Color(104, 182, 255),
		SynergyArmorPulseMode.Bell => new Color(255, 96, 48),
		SynergyArmorPulseMode.Storm => new Color(96, 224, 255),
		SynergyArmorPulseMode.Broker => new Color(255, 224, 96),
		SynergyArmorPulseMode.Frost => new Color(142, 228, 255),
		SynergyArmorPulseMode.Bloom => new Color(92, 255, 148),
		SynergyArmorPulseMode.Paradox => new Color(224, 112, 255),
		SynergyArmorPulseMode.Horizon => new Color(92, 62, 154),
		SynergyArmorPulseMode.Lunar => new Color(144, 172, 255),
		SynergyArmorPulseMode.Threshold => new Color(255, 245, 182),
		SynergyArmorPulseMode.Architect => new Color(255, 160, 88),
		_ => Color.White
	};
}
