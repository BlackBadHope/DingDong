using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

public sealed partial class DingDongHousingProbeCommand
{
	private void RunReplacementGallery(CommandCaller caller, string worldFile)
	{
		List<AuthoredPreset> presets = AuthoredReplacementProbeManifest.All.Where(preset => AuthoredReplacementHousingServerManifest.IsPassing(preset.Id)).ToList();
		if (presets.Count != AuthoredReplacementHousingServerManifest.PassingCount)
			throw new InvalidOperationException($"Replacement gallery requires {AuthoredReplacementHousingServerManifest.PassingCount} passing presets, found {presets.Count}.");
		const int columns = 4;
		const int gap = 20;
		int rows = (presets.Count + columns - 1) / columns;
		int slotWidth = presets.Max(preset => preset.Width) + gap;
		int slotHeight = presets.Max(preset => preset.Height) + gap;
		Rectangle gallery = FindDisposableEnvelope(columns * slotWidth + gap, rows * slotHeight + gap);
		ClearProbeArea(gallery);
		int errors = 0;
		Mod.Logger.Info($"DINGDONG_REPLACEMENT_GALLERY_BEGIN|world={worldFile}|presets={presets.Count}|gallery={gallery.X},{gallery.Y},{gallery.Width},{gallery.Height}");
		for (int index = 0; index < presets.Count; index++)
		{
			AuthoredPreset preset = presets[index];
			int column = index % columns;
			int row = index / columns;
			Point origin = new(gallery.X + gap + column * slotWidth + (slotWidth - gap - preset.Width) / 2, gallery.Y + gap + row * slotHeight + (slotHeight - gap - preset.Height) / 2);
			try
			{
				DingDongAuthoredStructureLoader.Generate(Mod, preset, origin);
				Mod.Logger.Info($"DINGDONG_REPLACEMENT_GALLERY|preset={preset.Id}|origin={origin.X},{origin.Y}|size={preset.Width}x{preset.Height}|validRooms={AuthoredReplacementHousingServerManifest.ValidRoomCount(preset.Id)}");
			}
			catch (Exception exception)
			{
				errors++;
				Mod.Logger.Error($"DINGDONG_REPLACEMENT_GALLERY|preset={preset.Id}|error={exception.GetType().Name}:{OneLine(exception.Message)}");
			}
		}

		string verdict = errors == 0 ? "GALLERY_READY" : "FAIL";
		string summary = $"DINGDONG_REPLACEMENT_GALLERY_END|verdict={verdict}|placed={presets.Count - errors}/{presets.Count}|errors={errors}|saveRequired=true";
		Mod.Logger.Info(summary);
		caller.Reply(summary, errors == 0 ? Color.LightGreen : Color.OrangeRed);
	}
}
