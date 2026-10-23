using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes.Items.AccessoryEnchanting;
public sealed partial class AccessoryEnchantGlobalItem : GlobalItem
{
	internal const byte MaxLevel = 5;
	private const string LevelKey = "dingDongAccessoryLevel";
	private const string ImprintKey = "dingDongAccessoryImprint";
	private const string FusedKey = "dingDongAccessoryFused";
	private List<AccessoryEnchantmentId> fused = new();
	public override bool InstancePerEntity => true;
	protected override bool CloneNewInstances => true;
	internal byte Level { get; private set; }
	internal AccessoryEnchantmentId Imprint { get; private set; }
	internal IReadOnlyList<AccessoryEnchantmentId> Fused => fused;
}
