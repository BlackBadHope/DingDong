using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.MobRelics;
internal readonly record struct MobRelicSourceDefinition(string ContentKey, string DisplayName, string Provider, bool Boss, string StableId);
