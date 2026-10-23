using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
public readonly record struct DamageSourceClassification(DamageSourceProfile Profile, DamageCoverageKind Coverage, bool TrueMultistrike);
