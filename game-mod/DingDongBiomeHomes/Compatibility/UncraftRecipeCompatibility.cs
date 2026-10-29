using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Compatibility;

public sealed class UncraftRecipeCompatibility : ModSystem
{
	private readonly List<ILHook> hooks = new();
	private static readonly Dictionary<int, Recipe> selected = new();
	private static MethodInfo find, uncraft;
	private static FieldInfo success, error;
	internal static Type ProviderType { get; private set; }
	internal static bool Installed { get; private set; }
	private static int suppressedGroups;

	public override void Load()
	{
		Type type = R19CompatibilitySystem.Provider("LansUncraftItems", "2023.10.8.1", "LansUncraftItems.LansUncraftItems");
		if (type == null) return;
		if (type.Module.ModuleVersionId != Guid.Parse("fda6c0ae-fb0e-44e4-80cf-2e32af4b7f72"))
			throw new InvalidOperationException("Uncraft recipe provider identity changed.");
		ProviderType = type;
		const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
		find = type.GetMethod("FindUncraftRecipes", flags, null, [typeof(Item)], null);
		uncraft = type.GetMethod("UncraftItem", flags, null, [typeof(Item), typeof(Recipe), typeof(bool)], null);
		Type result = uncraft.ReturnType;
		success = result.GetField("success"); error = result.GetField("errorReason");
		try
		{
			hooks.Add(new ILHook(find, il => {
				var c = new ILCursor(il);
				if (il.Body.Instructions.Count(i => i.OpCode == OpCodes.Ret) != 1)
					throw new InvalidOperationException("Uncraft recipe return contract changed.");
				c.Goto(il.Body.Instructions.Last());
				c.Emit(OpCodes.Ldarg_1);
				c.EmitDelegate<Func<List<Recipe>, Item, List<Recipe>>>(ChooseBasic);
			}));
			foreach (MethodInfo method in type.GetMethods(flags).Where(m => m.Name == "SwapItemGroup"))
				hooks.Add(new ILHook(method, il => {
					il.Body.Instructions.Clear(); il.Body.ExceptionHandlers.Clear(); il.Body.Variables.Clear();
					var c = new ILCursor(il); c.Emit(OpCodes.Ldc_I4_0); c.Emit(OpCodes.Ret);
				}));
			MethodInfo core = type.GetMethod("uncraftItem", flags, null, [typeof(Item), typeof(Recipe)], null);
			hooks.Add(new ILHook(core, il => {
				Instruction original = il.Body.Instructions[0]; var c = new ILCursor(il);
				c.Emit(OpCodes.Ldarg_0); c.Emit(OpCodes.Ldarg_1); c.Emit(OpCodes.Ldarg_2);
				c.EmitDelegate<Func<object, Item, Recipe, bool>>(IsSelected);
				c.Emit(OpCodes.Brtrue, original);
				c.Emit(OpCodes.Ldc_I4_0);
				c.EmitDelegate<Func<string>>(() => Language.GetTextValue("Mods.DingDongBiomeHomes.Uncraft.BasicOnly"));
				c.Emit(OpCodes.Newobj, il.Import(result.GetConstructor([typeof(bool), typeof(string)]))); c.Emit(OpCodes.Ret);
			}));
			MethodInfo quick = type.GetMethod("uncraftItem", flags, null, [typeof(Item), typeof(bool)], null);
			hooks.Add(new ILHook(quick, il => {
				il.Body.Instructions.Clear(); il.Body.ExceptionHandlers.Clear(); il.Body.Variables.Clear();
				var c = new ILCursor(il); c.Emit(OpCodes.Ldarg_0); c.Emit(OpCodes.Ldarg_1); c.Emit(OpCodes.Ldarg_2);
				c.EmitDelegate<Action<object, Item, bool>>(QuickUncraft); c.Emit(OpCodes.Ret);
			}));
			if (!Main.dedServ)
			{
				Type ui = type.Assembly.GetType("LansUncraftItems.LansUncraftItemsUI", true);
				hooks.Add(new ILHook(R19CompatibilitySystem.Method(ui, "RecreatePanel"), il => {
					var c = new ILCursor(il);
					if (!c.TryGotoNext(MoveType.After, i => i.MatchCallvirt<RecipeGroup>("ContainsItem")))
						throw new InvalidOperationException("Uncraft group UI contract changed.");
					// Recipe groups are crafting substitutes, not reversible exchanges.
					c.EmitDelegate<Func<bool, bool>>(matched => { if (matched) suppressedGroups++; return false; });
				}));
			}
			Installed = true;
		}
		catch { Unload(); throw; }
	}

	internal static List<Recipe> ChooseBasic(List<Recipe> candidates, Item input)
	{
		// Keep the provider's blocked/cyclic filters. Never manufacture a recipe
		// from a shared furniture/wood group or change a required ingredient ID.
		Recipe best = candidates.Where(r => r?.createItem?.type == input.type && UncraftReversibility.IsMaterialRecipe(r))
			.OrderBy(CostPerOutput).ThenBy(r => r.requiredItem.Sum(i => (long)Math.Max(0, i.rare)))
			.ThenBy(r => r.RecipeIndex).FirstOrDefault();
		selected[input.type] = best;
		return best == null ? new List<Recipe>() : new List<Recipe> { best };
	}
	private static decimal CostPerOutput(Recipe recipe) => recipe.requiredItem.Sum(
		i => (decimal)Math.Max(0, i.value) * Math.Max(0, i.stack)) / recipe.createItem.stack;

	private static bool IsSelected(object provider, Item input, Recipe recipe)
	{
		if (input == null || input.IsAir || !UncraftReversibility.IsMaterialRecipe(recipe)) return false;
		if (!selected.TryGetValue(input.type, out Recipe basic))
		{
			_ = find.Invoke(provider, [input]); selected.TryGetValue(input.type, out basic);
		}
		return ReferenceEquals(recipe, basic) && recipe.createItem.type == input.type;
	}
	private static void QuickUncraft(object provider, Item input, bool all)
	{
		var choices = (List<Recipe>)find.Invoke(provider, [input]);
		if (choices.Count == 0) { Main.NewText(Language.GetTextValue("Mods.DingDongBiomeHomes.Uncraft.NoBasicRecipe")); return; }
		object result = uncraft.Invoke(provider, [input, choices[0], all]);
		Recipe.FindRecipes();
		if (!(bool)success.GetValue(result)) Main.NewText((string)error.GetValue(result));
	}
	internal static Recipe BasicRecipe(Item input)
	{
		if (!Installed || input == null || input.IsAir) return null;
		if (!selected.TryGetValue(input.type, out Recipe basic))
		{
			_ = find.Invoke(ModLoader.GetMod("LansUncraftItems"), [input]);
			selected.TryGetValue(input.type, out basic);
		}
		return basic;
	}
	internal static bool TryBatch(Item input, out string reason)
	{
		reason = "";
		Recipe recipe = BasicRecipe(input);
		if (recipe == null || input.favorited || input.stack < recipe.createItem.stack) return false;
		object result = uncraft.Invoke(ModLoader.GetMod("LansUncraftItems"), [input, recipe, false]);
		reason = (string)error.GetValue(result);
		return (bool)success.GetValue(result);
	}
	internal static int CheckNativeClientPanel()
	{
		if (Main.dedServ || Environment.GetEnvironmentVariable("DINGDONG_UI_LAYOUT_PROBE") != "1")
			throw new InvalidOperationException("Explicit native UI probe required.");
		Type ui = ProviderType.Assembly.GetType("LansUncraftItems.LansUncraftItemsUI", true);
		object instance = ui.GetField("Instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
		Item previous = Main.mouseItem; int before = suppressedGroups;
		try
		{
			Main.mouseItem = new Item(Terraria.ID.ItemID.WoodenChair);
			R19CompatibilitySystem.Method(ui, "RecreatePanel").Invoke(instance, null);
			if (suppressedGroups <= before || ui.GetField("panel", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(instance) == null)
				throw new InvalidOperationException("Native furniture panel did not suppress crafting-group exchanges.");
			return 1;
		}
		finally { Main.mouseItem = previous; }
	}
	public override void Unload()
	{
		for (int i = hooks.Count - 1; i >= 0; i--) hooks[i].Dispose();
		hooks.Clear(); selected.Clear(); find = uncraft = null; success = error = null;
		UncraftReversibility.Reset();
		ProviderType = null; Installed = false; suppressedGroups = 0;
	}
}
