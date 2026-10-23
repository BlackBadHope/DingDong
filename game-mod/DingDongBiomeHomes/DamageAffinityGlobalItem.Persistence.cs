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
public sealed partial class DamageAffinityGlobalItem
{
	public override void SaveData(Item item, TagCompound tag)
	{
		// MagicStorage compares serialized GlobalItem data after CanStack.
		// The same fungible stack must have the same identity on both paths.
		tag[AcquisitionSaveKey] = StackAcquisition(item, AcquisitionTag);
		tag[ElementSaveKey] = ElementTag;
		tag[MetadataVersionSaveKey] = MetadataVersion;
	}

	public override void LoadData(Item item, TagCompound tag)
	{
		(string defaultAcquisition, string defaultElement) = DefaultsFor(item);
		AcquisitionTag = DamageTagWire.SanitizeAcquisition(tag.ContainsKey(AcquisitionSaveKey) ? tag.GetString(AcquisitionSaveKey) : defaultAcquisition, defaultAcquisition);
		AcquisitionTag = StackAcquisition(item, AcquisitionTag);
		ElementTag = DamageTagWire.SanitizeElement(tag.ContainsKey(ElementSaveKey) ? tag.GetString(ElementSaveKey) : defaultElement, defaultElement);
		// Legacy inferred defaults need a one-time migration. Newly saved explicit
		// elements must never be inferred again (e.g. Fire on a Frozen Turtle Shell).
		int savedVersion = tag.ContainsKey(MetadataVersionSaveKey) ? tag.GetInt(MetadataVersionSaveKey) : 0;
		if (savedVersion < 2)
			ElementTag = PersistedElement(item, ElementTag);
		// R30 saved these three owned closed-compound names as Neutral. Repair
		// only that old default; explicit non-neutral elements and foreign items
		// retain their saved identity. Version 2's legacy migration is not rerun.
		if (savedVersion < 3 && ElementTag == DamageTags.Neutral &&
			item.ModItem is Items.AiArsenal.CinderlockCarbine or Items.AiArsenal.CandlewickWand or Items.AiArsenal.HearthflameTome)
			ElementTag = DamageTags.Fire;
	}

	public override void NetSend(Item item, BinaryWriter writer)
	{
		DamageTagWire.Write(writer, StackAcquisition(item, AcquisitionTag));
		DamageTagWire.Write(writer, ElementTag);
	}

	public override void NetReceive(Item item, BinaryReader reader)
	{
		(string defaultAcquisition, string defaultElement) = DefaultsFor(item);
		AcquisitionTag = DamageTagWire.SanitizeAcquisition(DamageTagWire.Read(reader), defaultAcquisition);
		AcquisitionTag = StackAcquisition(item, AcquisitionTag);
		ElementTag = DamageTagWire.SanitizeElement(DamageTagWire.Read(reader), defaultElement);
	}

	private static string PersistedElement(Item item, string element)
	{
		// Repair only an exact old inferred default, including on existing gear.
		// Explicit staff defaults and different/custom elements are preserved;
		// acquisition, prefixes and foreign GlobalItem/enchantment data are untouched.
		if (item.type is ItemID.SlimeStaff or ItemID.BabyBirdStaff) return element;
		string identity = DamageRuntimeClassifier.StableItemIdentity(item);
		return string.Equals(element, DamageRuntimeClassifier.InferLegacyElement(identity), StringComparison.Ordinal)
			? DefaultsFor(item).Element : element;
	}

	private static string StackAcquisition(Item item, string acquisition) =>
		CanMergeOrigins(item) ? DefaultsFor(item).Acquisition : acquisition;
}
