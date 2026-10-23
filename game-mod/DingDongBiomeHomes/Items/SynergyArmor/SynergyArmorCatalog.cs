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
internal static partial class SynergyArmorCatalog
{
	internal const int ExpectedSetCount = 15;
	internal const int PiecesPerSet = 3;
	private static readonly Dictionary<SynergyArmorSetId, SynergyArmorSet> Sets = new();
	private static readonly Dictionary<string, SynergyArmorSetDefinition> DefinitionsByKey = new(StringComparer.Ordinal);
	private static readonly IReadOnlyList<SynergyArmorSetDefinition> Definitions = BuildDefinitions();
	internal static IReadOnlyDictionary<SynergyArmorSetId, SynergyArmorSet> RegisteredSets => Sets;
	internal static IReadOnlyList<SynergyArmorSetDefinition> AllDefinitions => Definitions;
}
