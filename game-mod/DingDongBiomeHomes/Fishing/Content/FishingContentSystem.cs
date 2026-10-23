using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Fishing.Content;

public static partial class FishingContentCatalog
{
	private static readonly Dictionary<string, GeneratedFishingItem> registered = new(StringComparer.Ordinal);

	public static int GetItemType(string id) => id != null && registered.TryGetValue(id, out GeneratedFishingItem item) ? item.Type : 0;
	public static bool TryGetItemType(string id, out int itemType) => (itemType = GetItemType(id)) > 0;
	public static bool TryGetDefinition(string id, out FishingContentDefinition definition)
	{
		definition = Items.FirstOrDefault(item => String.Equals(item.Id, id, StringComparison.Ordinal));
		return definition != null;
	}

	internal static void Register(Mod mod)
	{
		if (registered.Count > 0) throw new InvalidOperationException("Fishing content was registered twice.");
		IReadOnlyList<string> errors = ValidateDefinitions();
		if (errors.Count > 0) throw new InvalidOperationException("Invalid fishing content: " + string.Join("; ", errors));
		// Never sort Rigs: its append-only index is the player's save/network contract.
		foreach (FishingContentDefinition definition in Items.OrderBy(item => item.Id, StringComparer.Ordinal))
		{
			GeneratedFishingItem item = new(definition);
			if (!mod.AddContent(item)) throw new InvalidOperationException("Unable to register fishing item " + definition.Id);
			registered.Add(definition.Id, item);
		}
	}

	internal static string GroupName(string id) => "DingDongBiomeHomes:Fishing:" + id;
	internal static void RegisterRecipeGroups()
	{
		foreach (FishingRecipeGroupSpec definition in RecipeGroups)
		{
			FishingRecipeGroupSpec captured = definition;
			string key = "Mods.DingDongBiomeHomes.Fishing.Groups." + captured.Id;
			Language.GetOrRegister(key, () => captured.NameEn);
			RecipeGroup.RegisterGroup(GroupName(captured.Id), new RecipeGroup(() => Language.GetTextValue(key), captured.ItemIds.Select(GetItemType).ToArray()));
		}
	}

	internal static void Reset() => registered.Clear();
}

public sealed class FishingContentSystem : ModSystem
{
	public override void Load() => FishingContentCatalog.Register(Mod);
	public override void AddRecipeGroups() => FishingContentCatalog.RegisterRecipeGroups();
	public override void Unload() => FishingContentCatalog.Reset();
}
