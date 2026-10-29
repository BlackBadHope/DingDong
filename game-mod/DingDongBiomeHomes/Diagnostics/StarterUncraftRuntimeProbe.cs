using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using DingDongBiomeHomes.Compatibility;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Diagnostics;

public sealed class StarterUncraftRuntimeProbe : ModSystem
{
	public override void PostSetupRecipes()
	{
		string output = Environment.GetEnvironmentVariable("DINGDONG_STARTER_UNCRAFT_PROBE_OUTPUT");
		if (!Main.dedServ || string.IsNullOrEmpty(output)) return;
		var checks = new List<object>(); string error = null; int catalogItems = 0;
		void Require(bool ok, string name)
		{
			checks.Add(new { name, status = ok ? "PASS" : "FAIL" });
			if (!ok) throw new InvalidOperationException(name);
		}
		int originalPlayer = Main.myPlayer; Player saved = Main.player[0];
		try
		{
			Player player = new(); Main.player[0] = player; Main.myPlayer = 0;
			var starter = player.GetModPlayer<MagicStorageStarterPlayer>();
			Item[] kit = starter.AddStartingItems(false).ToArray();
			Require(kit.Length == 5 && kit.All(i => i.ModItem?.Mod.Name == "MagicStorage") && kit.All(i => i.ModItem.Name != "StorageUnitTiny"), "Starter contains only five Magic Storage component stacks, no Tiny Storage Unit");
			Require(!starter.AddStartingItems(true).Any(), "No kit on mediumcore death");
			Item[] creation = PlayerLoader.GetStartingItems(player, Array.Empty<Item>(), false).ToArray();
			foreach (Item component in kit)
				Require(creation.Where(i => i.type == component.type).Sum(i => i.stack) == component.stack,
					"Native character creation includes exact stack: " + component.ModItem.Name);
			Mod storage = ModLoader.GetMod("MagicStorage");
			int Capacity(string tier) => (int)storage.Code.GetType("MagicStorage.CrossMod.Default." + tier, true)
				.GetProperty("Capacity").GetValue(Activator.CreateInstance(storage.Code.GetType("MagicStorage.CrossMod.Default." + tier, true)));
			Require(Capacity("TierBasic") * 3 + Capacity("TierTiny") * 2 == 128, "Native storage tiers total exactly 128 item slots");
			Require(kit.Any(i => i.ModItem.Name == "StorageHeart") && kit.Any(i => i.ModItem.Name == "CraftingAccess") &&
				kit.Any(i => i.ModItem.Name == "StorageConnector"), "Kit includes heart, crafting access and connectors");
			Require(UncraftRecipeCompatibility.Installed, "Uncraft compatibility installed in native runtime");
			object provider = ModLoader.GetMod("LansUncraftItems"); Type type = provider.GetType();
			const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			MethodInfo find = type.GetMethod("FindUncraftRecipes", flags);
			bool[] cyclic = (bool[])type.GetField("cyclicRecipe", flags).GetValue(provider);
			Require(cyclic != null && cyclic.Length == Main.recipe.Length, "Native cyclic-recipe guard preserved");
			var blocked = new HashSet<Recipe>();
			foreach (object row in (IEnumerable)type.GetField("blockedRecipes", flags).GetValue(provider))
				blocked.Add((Recipe)row.GetType().GetField("recipe").GetValue(row));
			int[] types = Main.recipe.Where(r => r?.createItem?.type > 0).Select(r => r.createItem.type).Distinct().ToArray();
			bool single = true, exact = true, filters = true, cheapest = true;
			foreach (int itemType in types)
			{
				var choices = (List<Recipe>)find.Invoke(provider, [new Item(itemType)]); catalogItems++;
				single &= choices.Count <= 1;
				if (choices.Count == 0) continue;
				Recipe r = choices[0]; exact &= r.createItem.type == itemType;
				filters &= !cyclic[r.RecipeIndex] && !blocked.Contains(r);
				decimal Cost(Recipe recipe) => recipe.requiredItem.Sum(i => (decimal)Math.Max(0, i.value) * Math.Max(0, i.stack)) / recipe.createItem.stack;
				cheapest &= !Main.recipe.Where((v, index) => v?.createItem?.type == itemType && v.createItem.stack > 0 &&
					v.requiredItem.Count > 0 && !cyclic[index] && !blocked.Contains(v)).Any(v => Cost(v) < Cost(r));
			}
			Require(single && exact, "Every registered craftable item has at most one exact-item dismantling option");
			Require(filters, "Blocked and cyclic recipes remain blocked across catalogue");
			Require(cheapest, "Selected recipe uses lowest native ingredient value per output item");
			Item chair = new(ItemID.WoodenChair, 2); int chairType = chair.type;
			foreach (MethodInfo swap in type.GetMethods(flags).Where(m => m.Name == "SwapItemGroup"))
			{
				object[] args = swap.GetParameters().Length == 2 ? [chair, ItemID.GoldBar] : [chair, ItemID.GoldBar, true];
				Require(!(bool)swap.Invoke(provider, args) && chair.type == chairType && chair.stack == 2,
					"Group conversion refused without consuming chair: " + swap.GetParameters().Length + " arguments");
			}
			var chairChoices = (List<Recipe>)find.Invoke(provider, [chair]);
			Require(chairChoices.Count == 1 && chairChoices[0].requiredItem.All(i => i.type == ItemID.Wood), "Wooden chair dismantles only into wood");
			MethodInfo dismantle = type.GetMethod("UncraftItem", flags, null, [typeof(Item), typeof(Recipe), typeof(bool)], null);
			Recipe fake = (Recipe)typeof(object).GetMethod("MemberwiseClone", flags).Invoke(chairChoices[0], null);
			typeof(Recipe).GetField("requiredItem", flags).SetValue(fake, new List<Item> { new(ItemID.GoldBar, 50) });
			object denied = dismantle.Invoke(provider, [chair, fake, false]);
			Require(!(bool)denied.GetType().GetField("success").GetValue(denied) && chair.stack == 2 && player.inventory.All(i => i.IsAir),
				"Forged high-value recipe rejected at consumption boundary without inventory mutation");
			object result = dismantle.Invoke(provider, [chair, chairChoices[0], true]);
			Require((bool)result.GetType().GetField("success").GetValue(result) && chair.IsAir && player.inventory.Any(i => i.type == ItemID.Wood) &&
				player.inventory.All(i => i.IsAir || i.type == ItemID.Wood), "Native bulk dismantling consumes chairs and returns only wood");
		}
		catch (Exception ex) { error = ex.ToString(); }
		finally { Main.myPlayer = originalPlayer; Main.player[0] = saved; }
		File.WriteAllText(output, JsonSerializer.Serialize(new { status = error == null ? "PASS" : "FAIL", checks, catalogItems, error,
			userSavesUsed = false, worldLoaded = false }, new JsonSerializerOptions { WriteIndented = true }));
		Mod.Logger.Info("DINGDONG_STARTER_UNCRAFT_PROBE|verdict=" + (error == null ? "PASS" : "FAIL") + "|catalogItems=" + catalogItems);
		Environment.Exit(error == null ? 0 : 1);
	}
}
