using System;
using System.Collections.Generic;
using System.Linq;

namespace DingDongBiomeHomes.Fishing.Content;

public enum FishingContentKind : byte { Fish, Material, Food, Rig, Potion, Instrument, Treasure }

// IDs are the save/network contract. Display text and texture choices may change independently.
public sealed record FishingContentDefinition(string Id, string NameEn, string NameRu,
	FishingContentKind Kind, int TextureItem, int Value, int Rarity,
	string UseEn = "", string UseRu = "", int BuffType = 0, int BuffMinutes = 0,
	string RigId = "", string LootMaterial = "", FishingHabitat? Habitat = null)
{
	public string InternalName => "Fishing" + string.Concat(Id.Split('_').Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
}

public readonly record struct FishingIngredient(string ItemId, int VanillaItem, string Group, int Stack)
{
	public static FishingIngredient Local(string id, int stack = 1) => new(id, 0, "", stack);
	public static FishingIngredient Vanilla(int type, int stack = 1) => new("", type, "", stack);
	public static FishingIngredient FromGroup(string group, int stack = 1) => new("", 0, group, stack);
}

public sealed record FishingRecipeSpec(string ResultId, int Stack, int Tile, IReadOnlyList<FishingIngredient> Ingredients);
public sealed record FishingRecipeGroupSpec(string Id, string NameEn, string NameRu, IReadOnlyList<string> ItemIds);

public static partial class FishingContentCatalog
{
	private sealed class CatalogData
	{
		internal readonly List<FishingContentDefinition> Items = new();
		internal readonly List<FishingRewardSpec> Rewards = new();
		internal readonly List<FishingRigSpec> Rigs = new();
		internal readonly List<FishingRecipeSpec> Recipes = new();
		internal readonly List<FishingRecipeGroupSpec> Groups = new();
	}

	private static readonly CatalogData data = BuildData();
	public static IReadOnlyList<FishingContentDefinition> Items { get; } = data.Items.AsReadOnly();
	public static IReadOnlyList<FishingRewardSpec> Rewards { get; } = data.Rewards.AsReadOnly();
	public static IReadOnlyList<FishingRigSpec> Rigs { get; } = data.Rigs.AsReadOnly();
	public static IReadOnlyList<FishingRecipeSpec> Recipes { get; } = data.Recipes.AsReadOnly();
	public static IReadOnlyList<FishingRecipeGroupSpec> RecipeGroups { get; } = data.Groups.AsReadOnly();
	public static FishingRigSpec ResolveRig(string id) => data.Rigs.FirstOrDefault(rig => rig.Id == id);

	public static IReadOnlyList<string> ValidateDefinitions()
	{
		List<string> errors = new();
		HashSet<string> ids = new(StringComparer.Ordinal);
		foreach (FishingContentDefinition item in Items)
		{
			if (!ids.Add(item.Id)) errors.Add($"Duplicate item ID: {item.Id}");
			if (item.Id.Length == 0 || item.Id.Any(c => !(c is >= 'a' and <= 'z' || c is >= '0' and <= '9' || c == '_'))) errors.Add($"Invalid item ID: {item.Id}");
			if (item.NameEn.Length == 0 || item.NameRu.Length == 0 || item.TextureItem <= 0 || item.Value <= 0) errors.Add($"Incomplete item: {item.Id}");
		}
		if (Items.Select(item => item.InternalName).Distinct(StringComparer.Ordinal).Count() != Items.Count) errors.Add("Internal item name collision");
		foreach (FishingRewardSpec reward in Rewards)
		{
			if (!ids.Contains(reward.ItemId)) errors.Add($"Unknown reward item: {reward.ItemId}");
			if (reward.MinimumWaterDepth < 0 || reward.MaximumWaterDepth < reward.MinimumWaterDepth || reward.Weight <= 0 || reward.MinimumStack < 1 || reward.MaximumStack < reward.MinimumStack || reward.RequiredProgression is < 0 or > 6) errors.Add($"Invalid reward: {reward.ItemId}");
		}
		foreach (var group in Rewards.GroupBy(r => r.ItemId))
			if (group.Select(r => (r.MinimumStack, r.MaximumStack)).Distinct().Count() != 1)
				errors.Add("Inconsistent catch stack range: " + group.Key);
		HashSet<string> groups = RecipeGroups.Select(group => group.Id).ToHashSet(StringComparer.Ordinal);
		foreach (FishingRecipeGroupSpec group in RecipeGroups)
			foreach (string id in group.ItemIds)
				if (!ids.Contains(id)) errors.Add($"Unknown group member: {group.Id}/{id}");
		foreach (FishingRecipeSpec recipe in Recipes)
		{
			if (!ids.Contains(recipe.ResultId) || recipe.Stack < 1 || recipe.Ingredients.Count == 0) errors.Add($"Invalid recipe: {recipe.ResultId}");
			foreach (FishingIngredient ingredient in recipe.Ingredients)
			{
				int alternatives = (ingredient.ItemId.Length > 0 ? 1 : 0) + (ingredient.VanillaItem > 0 ? 1 : 0) + (ingredient.Group.Length > 0 ? 1 : 0);
				if (alternatives != 1 || ingredient.Stack < 1 || ingredient.ItemId.Length > 0 && !ids.Contains(ingredient.ItemId) || ingredient.Group.Length > 0 && !groups.Contains(ingredient.Group)) errors.Add($"Unresolved recipe ingredient: {recipe.ResultId}/{ingredient}");
			}
		}
		foreach (FishingContentDefinition material in Items.Where(item => item.Kind == FishingContentKind.Material || item.Kind == FishingContentKind.Fish))
			if (!Recipes.Any(recipe => recipe.Ingredients.Any(ingredient => ingredient.ItemId == material.Id || ingredient.Group.Length > 0 && RecipeGroups.Any(group => group.Id == ingredient.Group && group.ItemIds.Contains(material.Id))))) errors.Add($"No crafting use: {material.Id}");
		foreach (FishingContentDefinition item in Items.Where(item => item.Kind is FishingContentKind.Food or FishingContentKind.Potion or FishingContentKind.Instrument or FishingContentKind.Rig))
			if (!Recipes.Any(recipe => recipe.ResultId == item.Id)) errors.Add($"No acquisition recipe: {item.Id}");
		return errors.AsReadOnly();
	}
}
