using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.MobRelics;
internal sealed class MobRelicSet
{
	private readonly GeneratedMobRelicItem[] items;
	internal MobRelicSourceDefinition Source { get; }
	internal IReadOnlyList<GeneratedMobRelicItem> Items => items;
	internal GeneratedMobRelicItem CommonTrace => items[(int)MobRelicTier.CommonTrace];
	internal GeneratedMobRelicItem CommonCurio => items[(int)MobRelicTier.CommonCurio];
	internal GeneratedMobRelicItem ExpertSeal => items[(int)MobRelicTier.ExpertSeal];
	internal GeneratedMobRelicItem MasterRelic => items[(int)MobRelicTier.MasterRelic];
	internal GeneratedMobRelicItem LegendaryMyth => items[(int)MobRelicTier.LegendaryMyth];

	internal MobRelicSet(MobRelicSourceDefinition source, GeneratedMobRelicItem[] items)
	{
		Source = source;
		this.items = items;
	}
}
