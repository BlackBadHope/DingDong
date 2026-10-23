using System.Linq;

namespace DingDongBiomeHomes.Fishing.Content;

public static class FishingContentText
{
	public static string Tooltip(FishingContentDefinition item, bool russian)
	{
		string text = russian ? item.UseRu : item.UseEn;
		if (item.Kind == FishingContentKind.Rig)
		{
			FishingRigSpec rig = FishingContentCatalog.ResolveRig(item.RigId);
			text += russian ? $"\nГлубина крючка: до {rig.TargetDepthTiles} блоков\n+{rig.PowerBonus} к силе рыбалки" : $"\nHook depth: up to {rig.TargetDepthTiles} tiles\n+{rig.PowerBonus} fishing power";
			if (rig.RequiredProgression > 0) text += "\n" + Progression(rig.RequiredProgression, russian);
		}
		if (item.Kind == FishingContentKind.Fish)
		{
			FishingRewardSpec reward = FishingContentCatalog.Rewards.First(r => r.ItemId == item.Id);
			text += "\n" + Habitat(reward.Habitat, russian);
			text += russian ? $"; крючок на глубине {reward.MinimumWaterDepth}–{reward.MaximumWaterDepth} блоков" : $"; hook depth {reward.MinimumWaterDepth}–{reward.MaximumWaterDepth} tiles";
			if (reward.MapLayer.HasValue) text += "\n" + MapLayer(reward.MapLayer.Value, russian);
			if (reward.RequiredProgression > 0) text += "\n" + Progression(reward.RequiredProgression, russian);
			if (reward.NightOnly) text += russian ? "\nЛовится ночью" : "\nCaught at night";
			if (reward.RainOnly) text += russian ? "\nЛовится в дождь" : "\nCaught during rain";
			if (reward.EventId.Length > 0) text += "\n" + (russian ? "Во время события: " : "During: ") + Event(reward.EventId, russian);
		}
		return text;
	}

	public static string MapLayer(FishingMapLayer layer, bool ru) => layer switch
	{
		FishingMapLayer.Surface => ru ? "Поверхностный слой мира" : "Surface layer of the world",
		FishingMapLayer.Underground => ru ? "Подземный слой мира" : "Underground layer of the world",
		FishingMapLayer.Cavern => ru ? "Пещерный слой мира" : "Cavern layer of the world",
		_ => ru ? "Преисподняя" : "Underworld layer of the world"
	};

	public static string Habitat(FishingHabitat habitat, bool ru) => habitat switch
	{
		FishingHabitat.CoastalShoals => ru ? "Прибрежные отмели" : "Coastal shoals",
		FishingHabitat.OpenOcean => ru ? "Открытый океан" : "Open ocean",
		FishingHabitat.DeepOcean => ru ? "Глубокий океан" : "Deep ocean",
		FishingHabitat.Abyss => ru ? "Бездна" : "Abyss",
		FishingHabitat.CaveLake => ru ? "Пещерные озёра" : "Cave lakes",
		_ => ru ? "Подземные реки" : "Underground rivers"
	};

	public static string Progression(int progression, bool ru) => progression switch
	{
		1 => ru ? "После победы над Пожирателем миров или Мозгом Ктулху" : "After defeating the Eater of Worlds or Brain of Cthulhu",
		2 => ru ? "После победы над Скелетроном" : "After defeating Skeletron",
		3 => ru ? "В сложном режиме" : "In Hardmode",
		4 => ru ? "После победы над механическим боссом" : "After defeating a mechanical boss",
		5 => ru ? "После победы над Плантерой" : "After defeating Plantera",
		6 => ru ? "После победы над Лунным лордом" : "After defeating the Moon Lord",
		_ => ""
	};

	private static string Event(string id, bool ru) => id switch
	{
		"shoal_run" => ru ? "Ход прибрежной стаи" : "Shoal Run",
		"tidal_wash" => ru ? "Приливный вынос" : "Tidal Wash",
		"feeding_frenzy" => ru ? "Охотничья стая" : "Feeding Frenzy",
		"storm_drift" => ru ? "Штормовой дрейф" : "Storm Drift",
		"lantern_bloom" => ru ? "Пробуждение фонарей" : "Lantern Bloom",
		"pressure_surge" => ru ? "Глубинный выброс" : "Pressure Surge",
		"black_current" => ru ? "Чёрное течение" : "Black Current",
		"leviathan_wake" => ru ? "След древней глубины" : "Leviathan Wake",
		"crystal_hatch" => ru ? "Выход кристальных игл" : "Crystal Hatch",
		"mineral_bloom" => ru ? "Минеральное свечение" : "Mineral Bloom",
		"flash_flood" => ru ? "Подземный паводок" : "Flash Flood",
		"silt_run" => ru ? "Илистый поток" : "Silt Run",
		_ => id
	};
}
