using System;
using System.Collections.Generic;
using DingDongBiomeHomes.Fishing.Content;
using DingDongBiomeHomes.Fishing.Encounters;
using DingDongBiomeHomes.Fishing.Worldgen;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Fishing;

public sealed class FishingDepthDisplay : InfoDisplay
{
	public override string Texture => $"Terraria/Images/Item_{ItemID.DepthMeter}";
	public override bool Active() => Main.LocalPlayer.GetModPlayer<FishingPlayer>().DepthSurveyActive;
	public override string DisplayValue(ref Color displayColor, ref Color displayShadowColor)
	{
		bool ru = Language.ActiveCulture.Name == "ru-RU";
		var state = Main.LocalPlayer.GetModPlayer<FishingPlayer>();
		for (int i = 0; i < Main.maxProjectiles; i++)
		{
			var p = Main.projectile[i];
			if (!p.active || !p.bobber || p.owner != Main.myPlayer || !p.wet || p.ai[0] != 0) continue;
			var c = FishingEnvironment.AtPosition((int)(p.Center.X / 16), (int)(p.Center.Y / 16));
			string layer = LayerName(c.MapLayer, ru);
			return ru ? $"Под водой: {c.WaterDepthTiles}/{c.WaterColumnTiles} бл. · {layer}" :
				$"Submerged: {c.WaterDepthTiles}/{c.WaterColumnTiles} tiles · {layer}";
		}
		var rig = state.ValidRig(state.ActiveRigIndex);
		return ru ? $"Оснастка: до {rig.TargetDepthTiles} бл. · Найдено видов: {state.Discoveries.Count}" :
			$"Rig: up to {rig.TargetDepthTiles} tiles · Discoveries: {state.Discoveries.Count}";
	}
	internal static string LayerName(FishingMapLayer layer, bool ru) => !ru ? layer.ToString() : layer switch
	{
		FishingMapLayer.Surface => "Поверхность", FishingMapLayer.Underground => "Подземелье",
		FishingMapLayer.Cavern => "Пещеры", _ => "Преисподняя"
	};
}

public sealed class FishingSurveyTooltips : GlobalItem
{
	public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
	{
		if (item.type != FishingContentCatalog.GetItemType("depth_compass")) return;
		bool ru = Language.ActiveCulture.Name == "ru-RU";
		var state = Main.LocalPlayer.GetModPlayer<FishingPlayer>();
		tooltips.Add(new TooltipLine(Mod, "FishingDiscoveries", ru ?
			$"Исследовано видов улова: {state.Discoveries.Count}; поймано: {state.Catches}" :
			$"Catch discoveries: {state.Discoveries.Count}; catches: {state.Catches}"));
		var c = FishingEnvironment.AtPosition((int)(Main.LocalPlayer.Center.X / 16), (int)(Main.LocalPlayer.Center.Y / 16));
		string eventId = FishingEncounterSystem.GetEventId(c.Habitat);
		foreach (var spec in FishingEncounterSystem.Definitions)
			if (spec.Id == eventId) tooltips.Add(new TooltipLine(Mod, "FishingEvent", ru ? spec.NameRu : spec.NameEn));
		FishingWaterRegion? nearest = null;
		float distance = float.MaxValue;
		foreach (var region in FishingWaterWorldSystem.Regions)
		{
			if (region.Shape is not (FishingRegionShape.Lake or FishingRegionShape.River)) continue;
			var center = region.Bounds.Center.ToVector2() * 16;
			float d = Vector2.DistanceSquared(center, Main.LocalPlayer.Center);
			if (d < distance) { distance = d; nearest = region; }
		}
		if (nearest is { } waterway)
		{
			Vector2 delta = waterway.Bounds.Center.ToVector2() * 16 - Main.LocalPlayer.Center;
			string horizontal = delta.X >= 0 ? (ru ? "восток" : "east") : (ru ? "запад" : "west");
			string vertical = delta.Y >= 0 ? (ru ? "ниже" : "below") : (ru ? "выше" : "above");
			string range = distance > 16000 * 16000 ? (ru ? "далёкий" : "distant") : distance > 4000 * 4000 ? (ru ? "слабый" : "faint") : (ru ? "близкий" : "nearby");
			tooltips.Add(new TooltipLine(Mod, "FishingWaterEcho", ru ? $"Водный отклик: {range}, {horizontal}, {vertical}" : $"Water echo: {range}, {horizontal}, {vertical}"));
		}
	}
}
