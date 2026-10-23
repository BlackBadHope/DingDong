using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
public readonly record struct DamageDifficultyEntry(string Source, string Mode, string Detail, bool AdapterHealthy);
