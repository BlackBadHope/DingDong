using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

public sealed partial class DingDongHousingProbeCommand
{
	private void RunBaseline(CommandCaller caller, HousingProbeRequest request)
	{
		List<AuthoredPreset> presets = SelectPresets(request);
		ValidatePresetSelection(request, presets);
		Rectangle envelope = FindDisposableEnvelope(presets.Max(p => p.Width) + 40, presets.Max(p => p.Height) + 40);
		HousingProbeCounters counters = new();
		Mod.Logger.Info($"{request.Prefix}_BEGIN|world={request.WorldFile}|presets={presets.Count}|envelope={envelope.X},{envelope.Y},{envelope.Width},{envelope.Height}|masterMode={Main.masterMode}|getGoodWorld={Main.getGoodWorld}");
		foreach (AuthoredPreset preset in presets)
			ProbePreset(request, preset, envelope, counters);
		CompleteBaseline(caller, request, presets.Count, envelope, counters);
	}

	private void ProbePreset(HousingProbeRequest request, AuthoredPreset preset, Rectangle envelope, HousingProbeCounters counters)
	{
		ClearProbeArea(envelope);
		Rectangle area = new(envelope.X + 20, envelope.Y + 20, preset.Width, preset.Height);
		bool supplementExpected = request.NeedsSupplementCheck && AuthoredHousingPreflightManifest.RequiresFurnitureSupplement(preset.Id);
		using var containers = AuthoredNativeContainerTransaction.Begin();
		try
		{
			DingDongAuthoredStructureLoader.Generate(Mod, preset, area.Location);
			if (request.Mode == HousingProbeMode.FailedShells)
			{
				ReportHousingShells(request.Prefix, preset, area, counters);
				return;
			}

			HousingProbeResult result = AuthoredSettlementBuilder.ProbeHousing(area);
			if (result.ValidRooms > 0)
				counters.Pass++;
			else if (supplementExpected)
				counters.ExpectedRepair++;
			else
				counters.Regression++;
			Mod.Logger.Info($"{request.Prefix}|preset={preset.Id}|mode={preset.PlacementMode}|offlineRepair={supplementExpected}|validRooms={result.ValidRooms}|candidatePoints={result.CandidatePoints}|firstFailure={OneLine(result.FirstFailure)}");
		}
		catch (Exception exception)
		{
			counters.Errors++;
			Mod.Logger.Error($"{request.Prefix}|preset={preset.Id}|error={exception.GetType().Name}:{OneLine(exception.Message)}");
		}
	}
}
