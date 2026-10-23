using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes.Items.AccessoryEnchanting;
public sealed partial class AccessoryEnchantGlobalItem
{
	internal void SetImprintForProbe(AccessoryEnchantmentId imprint) => Imprint = AccessoryEnchantmentCatalog.IsValid(imprint) ? imprint : AccessoryEnchantmentId.None;
}
