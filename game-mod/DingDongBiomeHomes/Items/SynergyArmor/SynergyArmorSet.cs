using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DingDongBiomeHomes.Items.MobRelics;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.SynergyArmor;
internal sealed class SynergyArmorSet
{
	private readonly GeneratedSynergyArmorItem[] items;
	internal SynergyArmorSetDefinition Definition { get; }
	internal IReadOnlyList<GeneratedSynergyArmorItem> Items => items;
	internal GeneratedSynergyArmorItem Head => items[(int)SynergyArmorPiece.Head];
	internal GeneratedSynergyArmorItem Body => items[(int)SynergyArmorPiece.Body];
	internal GeneratedSynergyArmorItem Legs => items[(int)SynergyArmorPiece.Legs];

	internal SynergyArmorSet(SynergyArmorSetDefinition definition, GeneratedSynergyArmorItem[] items)
	{
		Definition = definition;
		this.items = items;
	}
}
