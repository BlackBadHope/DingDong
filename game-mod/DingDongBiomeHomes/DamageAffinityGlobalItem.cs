using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;
/// <summary>
/// Persistent per-item origin and element metadata. These values travel with
/// the concrete item stack instead of being recomputed from the current owner.
/// </summary>
public sealed partial class DamageAffinityGlobalItem : GlobalItem
{
	private const string AcquisitionSaveKey = "damageAcquisition";
	private const string ElementSaveKey = "damageElement";
	private const string MetadataVersionSaveKey = "damageAffinityVersion";
	private const int MetadataVersion = 3;
	public override bool InstancePerEntity => true;
	protected override bool CloneNewInstances => true;
	public string AcquisitionTag { get; private set; } = DamageTags.UnspecifiedAcquisition;
	public string ElementTag { get; private set; } = DamageTags.Neutral;
}
