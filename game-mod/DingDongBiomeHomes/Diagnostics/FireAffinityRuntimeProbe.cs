using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes.Diagnostics;

// Explicit disposable-profile regression probe; inactive in normal launchers.
public sealed class FireAffinityRuntimeProbe : ModSystem
{
	public override void PostAddRecipes()
	{
		string output = Environment.GetEnvironmentVariable("DINGDONG_FIRE_AFFINITY_PROBE_OUTPUT");
		if (!Main.dedServ || string.IsNullOrEmpty(output)) return;
		var results = new List<object>();
		void Require(bool ok, string name)
		{
			results.Add(new { name, status = ok ? "PASS" : "FAIL" });
			if (!ok) throw new InvalidOperationException(name);
		}
		string error = null;
		try
		{
			GlobalItem signature = ModLoader.GetMod("SignatureEquipment").GetContent<GlobalItem>()
				.Single(g => g.GetType().FullName == "SignatureEquipment.SignatureGlobalItem");
			const BindingFlags fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			FieldInfo level = signature.GetType().GetField("signatureLevel", fields);
			FieldInfo xp = signature.GetType().GetField("signatureExpCurrent", fields);
			foreach (string name in new[] { "CinderlockCarbine", "CandlewickWand", "HearthflameTome" })
			{
				Item item = new(ModContent.Find<ModItem>("DingDongBiomeHomes/" + name).Type);
				var affinity = item.GetGlobalItem<DamageAffinityGlobalItem>();
				Require(affinity.ElementTag == DamageTags.Fire, name + ": new default is Fire");
				foreach (int version in new[] { 0, 1, 2 })
				{
					affinity.LoadData(item, Saved(DamageTags.Neutral, version));
					Require(affinity.ElementTag == DamageTags.Fire, name + ": old Neutral v" + version + " migrates");
				}
				affinity.LoadData(item, Saved(DamageTags.Frost, 2));
				Require(affinity.ElementTag == DamageTags.Frost, name + ": explicit Frost v2 survives");
				affinity.LoadData(item, Saved(DamageTags.Neutral, 3));
				Require(affinity.ElementTag == DamageTags.Neutral, name + ": explicit Neutral v3 survives");
				affinity.LoadData(item, Saved(DamageTags.Neutral, 2));
				level.SetValue(item.GetGlobalItem(signature), 178);
				xp.SetValue(item.GetGlobalItem(signature), 155);
				item.prefix = PrefixID.Demonic;
				Item restored = ItemIO.Load(ItemIO.Save(item));
				Require(restored.GetGlobalItem<DamageAffinityGlobalItem>().ElementTag == DamageTags.Fire &&
					restored.GetGlobalItem<DamageAffinityGlobalItem>().AcquisitionTag == DamageTags.Crafted &&
					restored.prefix == item.prefix && (int)level.GetValue(restored.GetGlobalItem(signature)) == 178 &&
					(int)xp.GetValue(restored.GetGlobalItem(signature)) == 155, name + ": native ItemIO retains corrected element, origin, prefix and mastery");
				using var bytes = new MemoryStream();
				using (var writer = new BinaryWriter(bytes, System.Text.Encoding.UTF8, true)) ItemIO.Send(restored, writer, true, true);
				bytes.Position = 0;
				using var reader = new BinaryReader(bytes);
				Item received = ItemIO.Receive(reader, true, true);
				Require(bytes.Position == bytes.Length && received.GetGlobalItem<DamageAffinityGlobalItem>().ElementTag == DamageTags.Fire &&
					(int)level.GetValue(received.GetGlobalItem(signature)) == 178, name + ": native item network roundtrip preserves Fire and mastery");
			}
			Item shell = new(ItemID.FrozenTurtleShell);
			shell.GetGlobalItem<DamageAffinityGlobalItem>().LoadData(shell, Saved(DamageTags.Fire, 2));
			Require(shell.GetGlobalItem<DamageAffinityGlobalItem>().ElementTag == DamageTags.Fire, "Foreign/vanilla explicit Fire v2 is not re-inferred");
			foreach (string text in new[] { "SpiceStarter", "BeetleArmor", "ModWithFire/PlainWand" })
				Require(DamageRuntimeClassifier.InferElement(text) == DamageTags.Neutral, "No lexical false positive: " + text);
		}
		catch (Exception exception) { error = exception.ToString(); }
		File.WriteAllText(output, JsonSerializer.Serialize(new { status = error == null ? "PASS" : "FAIL", results, error, userSavesUsed = false }, new JsonSerializerOptions { WriteIndented = true }));
		Mod.Logger.Info("DINGDONG_FIRE_AFFINITY_PROBE|verdict=" + (error == null ? "PASS" : "FAIL") + "|checks=" + results.Count);
	}
	private static TagCompound Saved(string element, int version) => new()
	{
		["damageElement"] = element, ["damageAffinityVersion"] = version,
		["damageAcquisition"] = DamageTags.Crafted
	};
}
