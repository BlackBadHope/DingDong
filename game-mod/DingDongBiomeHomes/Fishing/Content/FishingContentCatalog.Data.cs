using System;
using System.Linq;
using Terraria.ID;

namespace DingDongBiomeHomes.Fishing.Content;

public static partial class FishingContentCatalog
{
	private static CatalogData BuildData()
	{
		CatalogData c = new();
		void Fish(string id, string en, string ru, int texture, FishingHabitat habitat, int min, int max, int progression, int power, int weight = 30, string eventId = "", bool night = false, bool rain = false, FishingMapLayer? mapLayer = null)
		{
			c.Items.Add(new(id, en, ru, FishingContentKind.Fish, texture, 300 + progression * 180 + min * 2, progression >= 3 ? ItemRarityID.Orange : ItemRarityID.Blue,
				"A fresh catch for expedition cooking", "Свежий улов для походной кухни", Habitat: habitat));
			c.Rewards.Add(new(id, habitat, min, max, progression, power, weight, 1, 2, eventId, night, rain, mapLayer));
		}
		// Eight species per habitat: depth, progression, weather, night and two local events.
		Fish("silver_sprat", "Silver sprat", "Серебристый шпрот", ItemID.Bass, FishingHabitat.CoastalShoals, 0, 40, 0, 10, 70, mapLayer: FishingMapLayer.Surface);
		Fish("sand_dab", "Sand dab", "Песчаная камбала", ItemID.Flounder, FishingHabitat.CoastalShoals, 4, 48, 0, 10, 65, mapLayer: FishingMapLayer.Surface);
		Fish("coral_mullet", "Coral mullet", "Коралловая кефаль", ItemID.RedSnapper, FishingHabitat.CoastalShoals, 8, 80, 0, 15, 50);
		Fish("tide_bream", "Tide bream", "Приливный лещ", ItemID.Salmon, FishingHabitat.CoastalShoals, 12, 80, 1, 20, 40);
		Fish("rain_whiting", "Rain whiting", "Дождевой мерланг", ItemID.Trout, FishingHabitat.CoastalShoals, 8, 96, 1, 20, 35, rain: true);
		Fish("moon_sardine", "Moon sardine", "Лунная сардина", ItemID.Tuna, FishingHabitat.CoastalShoals, 8, 96, 2, 25, 25, night: true);
		Fish("shoal_runner", "Shoal runner", "Стайный бегунок", ItemID.RedSnapper, FishingHabitat.CoastalShoals, 8, 80, 0, 15, 45, "shoal_run", mapLayer: FishingMapLayer.Surface);
		Fish("tidal_goby", "Tidal goby", "Приливный бычок", ItemID.Bass, FishingHabitat.CoastalShoals, 16, 128, 1, 20, 40, "tidal_wash");

		Fish("blue_skipjack", "Blue skipjack", "Синий полосатик", ItemID.Tuna, FishingHabitat.OpenOcean, 20, 160, 0, 15, 65, mapLayer: FishingMapLayer.Surface);
		Fish("glass_mackerel", "Glass mackerel", "Стеклянная скумбрия", ItemID.Trout, FishingHabitat.OpenOcean, 30, 200, 1, 20, 50, mapLayer: FishingMapLayer.Surface);
		Fish("sailfin_herring", "Sailfin herring", "Парусная сельдь", ItemID.Salmon, FishingHabitat.OpenOcean, 40, 240, 1, 25, 45);
		Fish("storm_albacore", "Storm albacore", "Штормовой тунец", ItemID.Tuna, FishingHabitat.OpenOcean, 40, 256, 2, 30, 30, rain: true);
		Fish("midnight_marlin", "Midnight marlin", "Полуночный марлин", ItemID.Swordfish, FishingHabitat.OpenOcean, 60, 320, 3, 40, 25, night: true);
		Fish("royal_mahi", "Royal mahi", "Королевская корифена", ItemID.RedSnapper, FishingHabitat.OpenOcean, 80, 320, 4, 45, 20);
		Fish("frenzy_tuna", "Frenzy tuna", "Неистовый тунец", ItemID.Tuna, FishingHabitat.OpenOcean, 20, 256, 1, 25, 40, "feeding_frenzy");
		Fish("drift_swordfish", "Drift swordfish", "Дрейфующая рыба-меч", ItemID.Swordfish, FishingHabitat.OpenOcean, 40, 320, 3, 35, 35, "storm_drift");

		Fish("lantern_cod", "Lantern cod", "Фонарная треска", ItemID.SpecularFish, FishingHabitat.DeepOcean, 80, 512, 2, 30, 60);
		Fish("pressure_hake", "Pressure hake", "Глубинный хек", ItemID.ArmoredCavefish, FishingHabitat.DeepOcean, 100, 640, 2, 35, 50);
		Fish("cobalt_halibut", "Cobalt halibut", "Кобальтовый палтус", ItemID.Flounder, FishingHabitat.DeepOcean, 120, 800, 3, 40, 40);
		Fish("midnight_angler", "Midnight angler", "Полуночный удильщик", ItemID.SpecularFish, FishingHabitat.DeepOcean, 160, 900, 3, 45, 30, night: true);
		Fish("tempest_haddock", "Tempest haddock", "Буревая пикша", ItemID.Trout, FishingHabitat.DeepOcean, 120, 700, 4, 45, 25, rain: true);
		Fish("starfin_coelacanth", "Starfin coelacanth", "Звёздная латимерия", ItemID.ArmoredCavefish, FishingHabitat.DeepOcean, 200, 1000, 5, 55, 15);
		Fish("bloom_jellyfish", "Bloom jellyfish", "Цветущая медуза", ItemID.PinkJellyfish, FishingHabitat.DeepOcean, 80, 640, 2, 35, 35, "lantern_bloom");
		Fish("surge_sturgeon", "Surge sturgeon", "Напорный осётр", ItemID.Salmon, FishingHabitat.DeepOcean, 160, 1000, 4, 50, 30, "pressure_surge");

		Fish("blind_chimaera", "Blind chimaera", "Слепая химера", ItemID.ArmoredCavefish, FishingHabitat.Abyss, 160, 4096, 3, 40, 60);
		Fish("void_lanternfish", "Void lanternfish", "Пустотный светляк", ItemID.SpecularFish, FishingHabitat.Abyss, 240, 4096, 3, 45, 40, night: true);
		Fish("basalt_eel", "Basalt eel", "Базальтовый угорь", ItemID.Obsidifish, FishingHabitat.Abyss, 320, 4096, 4, 50, 40);
		Fish("obsidian_grenadier", "Obsidian grenadier", "Обсидиановый макрурус", ItemID.Obsidifish, FishingHabitat.Abyss, 400, 4096, 4, 55, 30);
		Fish("ancient_sleeper", "Ancient sleeper", "Древний сонник", ItemID.ArmoredCavefish, FishingHabitat.Abyss, 512, 4096, 5, 60, 20);
		Fish("eclipse_leviathan", "Eclipse leviathan", "Левиафан затмения", ItemID.Swordfish, FishingHabitat.Abyss, 512, 4096, 6, 70, 10, night: true);
		Fish("current_fangfish", "Black-current fangfish", "Клыкач чёрного течения", ItemID.Obsidifish, FishingHabitat.Abyss, 240, 4096, 4, 50, 35, "black_current");
		Fish("wake_oarfish", "Wake oarfish", "Сельдяной король следа", ItemID.Swordfish, FishingHabitat.Abyss, 512, 4096, 5, 65, 25, "leviathan_wake");

		Fish("limestone_carp", "Limestone carp", "Известняковый карп", ItemID.Bass, FishingHabitat.CaveLake, 8, 160, 0, 10, 70, mapLayer: FishingMapLayer.Underground);
		Fish("amethyst_loach", "Amethyst loach", "Аметистовый голец", ItemID.SpecularFish, FishingHabitat.CaveLake, 12, 200, 0, 15, 60, mapLayer: FishingMapLayer.Underground);
		Fish("cave_perch", "Cave perch", "Пещерный окунь", ItemID.ArmoredCavefish, FishingHabitat.CaveLake, 20, 240, 1, 20, 50, mapLayer: FishingMapLayer.Cavern);
		Fish("silver_blindfish", "Silver blindfish", "Серебряная слепоглазка", ItemID.SpecularFish, FishingHabitat.CaveLake, 30, 320, 1, 25, 40, mapLayer: FishingMapLayer.Cavern);
		Fish("rainstone_eel", "Rainstone eel", "Угорь дождевого камня", ItemID.Trout, FishingHabitat.CaveLake, 40, 400, 2, 30, 30, rain: true, mapLayer: FishingMapLayer.Underground);
		Fish("moonstone_koi", "Moonstone koi", "Луннокаменный кои", ItemID.Salmon, FishingHabitat.CaveLake, 64, 512, 3, 40, 25, night: true, mapLayer: FishingMapLayer.Cavern);
		Fish("crystal_fry", "Crystal fry", "Хрустальный малёк", ItemID.SpecularFish, FishingHabitat.CaveLake, 20, 320, 1, 20, 40, "crystal_hatch", mapLayer: FishingMapLayer.Underground);
		Fish("mineral_sturgeon", "Mineral sturgeon", "Минеральный осётр", ItemID.ArmoredCavefish, FishingHabitat.CaveLake, 80, 768, 4, 45, 30, "mineral_bloom", mapLayer: FishingMapLayer.Cavern);

		Fish("silt_dace", "Silt dace", "Илистый елец", ItemID.Bass, FishingHabitat.UndergroundRiver, 4, 80, 0, 10, 70, mapLayer: FishingMapLayer.Underground);
		Fish("copper_barbel", "Copper barbel", "Медный усач", ItemID.RedSnapper, FishingHabitat.UndergroundRiver, 8, 160, 0, 15, 60, mapLayer: FishingMapLayer.Underground);
		Fish("current_trout", "Current trout", "Быстрая форель", ItemID.Trout, FishingHabitat.UndergroundRiver, 12, 240, 0, 15, 50, mapLayer: FishingMapLayer.Underground);
		Fish("amber_pike", "Amber pike", "Янтарная щука", ItemID.Salmon, FishingHabitat.UndergroundRiver, 20, 320, 1, 25, 40, mapLayer: FishingMapLayer.Cavern);
		Fish("flood_catfish", "Flood catfish", "Паводковый сом", ItemID.Bass, FishingHabitat.UndergroundRiver, 30, 400, 2, 30, 30, rain: true, mapLayer: FishingMapLayer.Cavern);
		Fish("nocturnal_lamprey", "Nocturnal lamprey", "Ночная минога", ItemID.SpecularFish, FishingHabitat.UndergroundRiver, 40, 512, 3, 40, 25, night: true, mapLayer: FishingMapLayer.Cavern);
		Fish("flood_runner", "Flood runner", "Паводковый бегунок", ItemID.Trout, FishingHabitat.UndergroundRiver, 16, 320, 1, 25, 40, "flash_flood", mapLayer: FishingMapLayer.Underground);
		Fish("siltfin_gar", "Siltfin gar", "Илопёрый панцирник", ItemID.ArmoredCavefish, FishingHabitat.UndergroundRiver, 64, 640, 4, 45, 30, "silt_run", mapLayer: FishingMapLayer.Cavern);

		AddMaterials(c);
		AddRigs(c);
		AddFood(c);
		AddPotionsAndInstruments(c);
		NormalizeCatchStacks(c);
		return c;
	}
	private static void NormalizeCatchStacks(CatalogData c)
	{
		// One item has one stack range. Intersect alternate habitat rules so the
		// result is valid for every rule without per-catch metadata splitting stacks.
		foreach (var group in c.Rewards.GroupBy(r => r.ItemId))
		{
			int minimum = group.Max(r => r.MinimumStack), maximum = group.Min(r => r.MaximumStack);
			if (minimum > maximum) throw new InvalidOperationException("Conflicting catch stack ranges: " + group.Key);
			for (int i = 0; i < c.Rewards.Count; i++)
				if (c.Rewards[i].ItemId == group.Key)
					c.Rewards[i] = c.Rewards[i] with { MinimumStack = minimum, MaximumStack = maximum };
		}
	}

	private static void AddMaterials(CatalogData c)
	{
		void Material(string id, string en, string ru, int texture, FishingHabitat habitat, int depth, int progression, string enUse, string ruUse)
		{
			c.Items.Add(new(id, en, ru, FishingContentKind.Material, texture, 800 + progression * 400, ItemRarityID.Blue, enUse, ruUse, Habitat: habitat));
			c.Rewards.Add(new(id, habitat, depth, 4096, progression, 15 + progression * 5, 14, 1, 3));
		}
		Material("salvage_fragment", "Salvage fragment", "Обломок снаряжения", ItemID.IronBar, FishingHabitat.CaveLake, 8, 0, "Used for fishing rigs and expedition instruments", "Для поплавков и походных приборов");
		Material("tide_pearl", "Tide pearl", "Приливная жемчужина", ItemID.PearlstoneBlock, FishingHabitat.CoastalShoals, 0, 0, "Used for shoal rigs and fishing tonic", "Для мелководной оснастки и рыбацкого настоя");
		Material("coral_fiber", "Coral fiber", "Коралловое волокно", ItemID.Coral, FishingHabitat.CoastalShoals, 0, 0, "Used for fishing line and shoal cooking", "Для лески и прибрежных блюд");
		Material("ocean_glass", "Ocean glass", "Морское стекло", ItemID.Glass, FishingHabitat.OpenOcean, 8, 0, "Used for deep rigs and sonar tonic", "Для глубинной оснастки и настоя эхолокации");
		Material("storm_scale", "Storm scale", "Штормовая чешуя", ItemID.SharkFin, FishingHabitat.OpenOcean, 20, 1, "Used for ocean rigs and treasure tonic", "Для морской оснастки и настоя сокровищ");
		Material("pressure_crystal", "Pressure crystal", "Кристалл давления", ItemID.CrystalShard, FishingHabitat.DeepOcean, 40, 2, "Used for pressure rigs and diving equipment", "Для глубинной оснастки и водолазного снаряжения");
		Material("lantern_oil", "Lantern oil", "Фонарное масло", ItemID.BottledWater, FishingHabitat.DeepOcean, 80, 2, "Used for a diving lantern and sonar tonic", "Для водолазного фонаря и настоя эхолокации");
		Material("abyssal_steel", "Abyssal steel", "Сталь бездны", ItemID.Obsidian, FishingHabitat.Abyss, 80, 3, "Used for abyss rigs and diving equipment", "Для оснастки бездны и водолазного снаряжения");
		Material("void_pearl", "Void pearl", "Пустотная жемчужина", ItemID.BlackPearl, FishingHabitat.Abyss, 160, 4, "Used for the deepest rig and treasure tonic", "Для самой глубокой оснастки и настоя сокровищ");
		Material("cave_salt", "Cave salt", "Пещерная соль", ItemID.Amethyst, FishingHabitat.CaveLake, 8, 0, "Used in expedition meals and cave-sight tonic", "Для походной кухни и настоя рудознатца");
		Material("river_amber", "River amber", "Речной янтарь", ItemID.Amber, FishingHabitat.UndergroundRiver, 8, 0, "Used for sounding instruments and river tonic", "Для измерительных приборов и речного настоя");
		Material("silt_essence", "Silt essence", "Иловый экстракт", ItemID.SiltBlock, FishingHabitat.UndergroundRiver, 20, 1, "Used for strong fishing line and river tonic", "Для прочной лески и речного настоя");
		foreach (FishingHabitat habitat in Enum.GetValues<FishingHabitat>())
			if (habitat != FishingHabitat.CaveLake) c.Rewards.Add(new("salvage_fragment", habitat, 0, 4096, 0, 10, 10, 1, 3));
		// Bootstrap without buying a later rig or relying on combat drops. Existing-world
		// habitat fallback changes at hook depths 12 and 48, so shoreward traces bridge it.
		c.Rewards.Add(new("ocean_glass", FishingHabitat.CoastalShoals, 8, 11, 0, 15, 8, 1, 2));
		c.Rewards.Add(new("pressure_crystal", FishingHabitat.OpenOcean, 40, 47, 2, 25, 8, 1, 2));
		c.Rewards.Add(new("abyssal_steel", FishingHabitat.DeepOcean, 80, 159, 3, 30, 8, 1, 2));
		c.Rewards.Add(new("void_pearl", FishingHabitat.DeepOcean, 160, 319, 4, 35, 8, 1, 2));
		c.Rewards.Add(new("river_amber", FishingHabitat.CaveLake, 8, 4096, 0, 15, 8, 1, 2));
		(string Id, string En, string Ru, FishingHabitat Habitat, string Material, int Depth, int Progression)[] caches =
		{
			("shoal_cache", "Shoal salvage cache", "Тайник отмели", FishingHabitat.CoastalShoals, "tide_pearl", 8, 0),
			("ocean_cache", "Ocean salvage cache", "Морской тайник", FishingHabitat.OpenOcean, "storm_scale", 20, 1),
			("deep_cache", "Deepwater salvage cache", "Глубоководный тайник", FishingHabitat.DeepOcean, "pressure_crystal", 80, 2),
			("abyss_cache", "Abyss salvage cache", "Тайник бездны", FishingHabitat.Abyss, "abyssal_steel", 160, 3),
			("lake_cache", "Cave-lake salvage cache", "Тайник пещерного озера", FishingHabitat.CaveLake, "cave_salt", 12, 0),
			("river_cache", "River salvage cache", "Речной тайник", FishingHabitat.UndergroundRiver, "river_amber", 8, 0)
		};
		foreach (var cache in caches)
		{
			c.Items.Add(new(cache.Id, cache.En, cache.Ru, FishingContentKind.Treasure, ItemID.WoodenCrate, 5000 + cache.Progression * 2500, ItemRarityID.Green,
				"Right-click to recover useful materials, coins and supplies", "Правый клик: извлечь материалы, монеты и припасы", LootMaterial: cache.Material, Habitat: cache.Habitat));
			c.Rewards.Add(new(cache.Id, cache.Habitat, cache.Depth, 4096, cache.Progression, 20 + cache.Progression * 5, 5, 1, 1));
		}
	}

	private static void AddRigs(CatalogData c)
	{
		void Rig(string id, string en, string ru, int depth, int minimumDepth, int progression, int power, params FishingIngredient[] ingredients)
		{
			c.Rigs.Add(new(id, en, ru, depth, minimumDepth, progression, power, 1500 + depth * 20));
			c.Items.Add(new(id, en, ru, FishingContentKind.Rig, ItemID.FishingBobber, 1500 + depth * 20, progression >= 3 ? ItemRarityID.LightRed : ItemRarityID.Blue,
				"Right-click in your inventory to select this reusable rig", "Правый клик в инвентаре: выбрать этот многоразовый поплавок", RigId: id));
			c.Recipes.Add(new(id, 1, TileID.Anvils, ingredients));
		}
		Rig("rig_surface", "Surface float", "Поверхностный поплавок", 0, 0, 0, 0, FishingIngredient.Vanilla(ItemID.Wood, 10), FishingIngredient.Vanilla(ItemID.Gel, 2));
		Rig("rig_shoal", "Shoal float", "Поплавок отмели", 8, 8, 0, 1, FishingIngredient.Local("rig_surface"), FishingIngredient.Local("coral_fiber", 3), FishingIngredient.Local("tide_pearl", 2));
		Rig("rig_shelf", "Shelf float", "Шельфовый поплавок", 20, 20, 0, 2, FishingIngredient.Local("rig_shoal"), FishingIngredient.Local("ocean_glass", 3), FishingIngredient.Local("salvage_fragment", 5));
		Rig("rig_open_water", "Open-water float", "Морской поплавок", 40, 40, 1, 3, FishingIngredient.Local("rig_shelf"), FishingIngredient.Local("storm_scale", 4), FishingIngredient.Local("river_amber", 3));
		Rig("rig_deep", "Deepwater float", "Глубоководный поплавок", 80, 80, 2, 4, FishingIngredient.Local("rig_open_water"), FishingIngredient.Local("pressure_crystal", 5), FishingIngredient.Local("salvage_fragment", 8));
		Rig("rig_trench", "Trench float", "Поплавок впадины", 160, 160, 3, 5, FishingIngredient.Local("rig_deep"), FishingIngredient.Local("abyssal_steel", 6), FishingIngredient.Vanilla(ItemID.SoulofNight, 5));
		Rig("rig_abyss", "Abyss float", "Поплавок бездны", 320, 320, 4, 6, FishingIngredient.Local("rig_trench"), FishingIngredient.Local("void_pearl", 5), FishingIngredient.Vanilla(ItemID.HallowedBar, 5));
		Rig("rig_hadal", "Hadal float", "Поплавок предельных глубин", 512, 512, 5, 8, FishingIngredient.Local("rig_abyss"), FishingIngredient.Local("void_pearl", 8), FishingIngredient.Vanilla(ItemID.ChlorophyteBar, 5));
	}

	private static void AddFood(CatalogData c)
	{
		void Dish(string id, string en, string ru, FishingHabitat habitat, int half, int buff, string enEffect, string ruEffect, string spice)
		{
			string groupId = "fish_for_" + id;
			string[] fish = c.Items.Where(item => item.Kind == FishingContentKind.Fish && item.Habitat == habitat).Skip(half * 4).Take(4).Select(item => item.Id).ToArray();
			c.Groups.Add(new(groupId, "Catch for " + en.ToLowerInvariant(), "Улов для блюда «" + ru + "»", fish));
			c.Items.Add(new(id, en, ru, FishingContentKind.Food, ItemID.CookedFish, 2000, ItemRarityID.Blue,
				"Medium improvements to all stats for 12 minutes\n" + enEffect + " for 8 minutes", "Среднее улучшение всех характеристик на 12 мин.\n" + ruEffect + " на 8 мин.", buff, 8));
			c.Recipes.Add(new(id, 2, TileID.CookingPots, new[] { FishingIngredient.FromGroup(groupId, 2), FishingIngredient.Local(spice), FishingIngredient.Vanilla(ItemID.Mushroom, 2) }));
		}
		Dish("shoal_skewer", "Shoal skewer", "Шашлычок отмели", FishingHabitat.CoastalShoals, 0, BuffID.Fishing, "+15 fishing power", "+15 к силе рыбалки", "coral_fiber");
		Dish("tide_chowder", "Tide chowder", "Приливная похлёбка", FishingHabitat.CoastalShoals, 1, BuffID.WaterWalking, "Walk on water", "Хождение по воде", "cave_salt");
		Dish("ocean_ceviche", "Ocean ceviche", "Морское севиче", FishingHabitat.OpenOcean, 0, BuffID.Sonar, "See what is biting", "Показывает, что клюёт", "cave_salt");
		Dish("mariners_platter", "Mariner's platter", "Тарелка мореплавателя", FishingHabitat.OpenOcean, 1, BuffID.Crate, "Increased chance of fishing up crates", "Повышенный шанс выловить ящик", "storm_scale");
		Dish("lantern_broth", "Lantern broth", "Фонарный бульон", FishingHabitat.DeepOcean, 0, BuffID.NightOwl, "Improved night vision", "Улучшенное ночное зрение", "cave_salt");
		Dish("deepwater_pie", "Deepwater pie", "Глубоководный пирог", FishingHabitat.DeepOcean, 1, BuffID.Gills, "Breathe water", "Дыхание под водой", "cave_salt");
		Dish("abyssal_hotpot", "Abyssal hotpot", "Горячий котёл бездны", FishingHabitat.Abyss, 0, BuffID.ObsidianSkin, "Immunity to lava", "Невосприимчивость к лаве", "cave_salt");
		Dish("blackwater_roast", "Blackwater roast", "Жаркое чёрных вод", FishingHabitat.Abyss, 1, BuffID.Ironskin, "+8 defense", "+8 к защите", "cave_salt");
		Dish("crystal_fillet", "Crystal fillet", "Хрустальное филе", FishingHabitat.CaveLake, 0, BuffID.Spelunker, "Nearby treasure is highlighted", "Подсвечивает ближайшие сокровища", "cave_salt");
		Dish("cavefish_stew", "Cavefish stew", "Пещерная уха", FishingHabitat.CaveLake, 1, BuffID.Dangersense, "Nearby hazards are highlighted", "Подсвечивает ближайшие опасности", "cave_salt");
		Dish("river_fishcakes", "River fishcakes", "Речные рыбные котлеты", FishingHabitat.UndergroundRiver, 0, BuffID.Swiftness, "+25% movement speed", "+25% к скорости передвижения", "cave_salt");
		Dish("amber_bouillabaisse", "Amber bouillabaisse", "Янтарный буйабес", FishingHabitat.UndergroundRiver, 1, BuffID.Regeneration, "Increased life regeneration", "Ускоренное восстановление здоровья", "river_amber");
	}

	private static void AddPotionsAndInstruments(CatalogData c)
	{
		void Potion(string id, string en, string ru, int texture, int buff, int minutes, string enEffect, string ruEffect, string material, string fish)
		{
			c.Items.Add(new(id, en, ru, FishingContentKind.Potion, texture, 1200, ItemRarityID.Blue, enEffect + " for " + minutes + " minutes", ruEffect + " на " + minutes + " мин.", buff, minutes));
			c.Recipes.Add(new(id, 2, TileID.Bottles, new[] { FishingIngredient.Vanilla(ItemID.BottledWater, 2), FishingIngredient.Local(material, 2), FishingIngredient.Local(fish), FishingIngredient.Vanilla(ItemID.Daybloom) }));
		}
		Potion("shoal_tonic", "Shoal tonic", "Настой отмели", ItemID.FishingPotion, BuffID.Fishing, 8, "+15 fishing power", "+15 к силе рыбалки", "tide_pearl", "silver_sprat");
		Potion("ocean_breath_tonic", "Ocean-breath tonic", "Настой морского дыхания", ItemID.GillsPotion, BuffID.Gills, 8, "Breathe water", "Дыхание под водой", "ocean_glass", "blue_skipjack");
		Potion("deep_echo_tonic", "Deep-echo tonic", "Настой глубинного эха", ItemID.SonarPotion, BuffID.Sonar, 8, "See what is biting", "Показывает, что клюёт", "lantern_oil", "lantern_cod");
		Potion("treasure_current_tonic", "Treasure-current tonic", "Настой течения сокровищ", ItemID.CratePotion, BuffID.Crate, 5, "Increased chance of fishing up crates", "Повышенный шанс выловить ящик", "void_pearl", "blind_chimaera");
		Potion("cave_sight_tonic", "Cave-sight tonic", "Настой пещерного зрения", ItemID.SpelunkerPotion, BuffID.Spelunker, 8, "Nearby treasure is highlighted", "Подсвечивает ближайшие сокровища", "cave_salt", "limestone_carp");
		Potion("river_stride_tonic", "River-stride tonic", "Настой речной поступи", ItemID.WaterWalkingPotion, BuffID.WaterWalking, 8, "Walk on water", "Хождение по воде", "silt_essence", "silt_dace");
		c.Items.Add(new("depth_compass", "Depth compass", "Глубинный компас", FishingContentKind.Instrument, ItemID.DepthMeter, 12000, ItemRarityID.Green, "Shows depth while carried", "Показывает глубину, пока лежит в инвентаре"));
		c.Recipes.Add(new("depth_compass", 1, TileID.Anvils, new[] { FishingIngredient.Local("salvage_fragment", 8), FishingIngredient.Local("river_amber", 4), FishingIngredient.Local("ocean_glass", 3) }));
		c.Items.Add(new("sounding_line", "Sounding line", "Промерная леска", FishingContentKind.Instrument, ItemID.HighTestFishingLine, 18000, ItemRarityID.Green, "+10 fishing power\nPrevents fishing line from breaking", "+10 к силе рыбалки\nЛеска не рвётся"));
		c.Recipes.Add(new("sounding_line", 1, TileID.Anvils, new[] { FishingIngredient.Local("coral_fiber", 8), FishingIngredient.Local("silt_essence", 5), FishingIngredient.Local("storm_scale", 4) }));
		c.Items.Add(new("diving_lantern", "Diving lantern", "Водолазный фонарь", FishingContentKind.Instrument, ItemID.DivingHelmet, 25000, ItemRarityID.Orange, "Greatly extends underwater breathing\nLights the water around you", "Намного продлевает дыхание под водой\nОсвещает воду вокруг вас"));
		c.Recipes.Add(new("diving_lantern", 1, TileID.Anvils, new[] { FishingIngredient.Local("pressure_crystal", 5), FishingIngredient.Local("abyssal_steel", 4), FishingIngredient.Local("lantern_oil", 5) }));
	}
}
