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
internal readonly record struct SynergyArmorIngredient(int ItemType, int Head, int Body, int Legs)
{
	internal int Count(SynergyArmorPiece piece) => piece switch
	{
		SynergyArmorPiece.Head => Head,
		SynergyArmorPiece.Body => Body,
		_ => Legs
	};
}
