using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

public sealed partial class DingDongHousingProbeCommand
{
	private void ReportHousingShells(string prefix, AuthoredPreset preset, Rectangle area, HousingProbeCounters counters)
	{
		List<HousingShellProbe> shells = AuthoredSettlementBuilder.ProbeHousingShells(area);
		if (shells.Count > 0)
			counters.Pass++;
		else
			counters.Regression++;
		if (shells.Count == 0)
			Mod.Logger.Info($"{prefix}|preset={preset.Id}|shells=0");
		foreach (HousingShellProbe shell in shells)
		{
			Rectangle relative = new(shell.Bounds.X - area.X, shell.Bounds.Y - area.Y, shell.Bounds.Width, shell.Bounds.Height);
			Mod.Logger.Info($"{prefix}|preset={preset.Id}|bounds={relative.X},{relative.Y},{relative.Width},{relative.Height}|needs={shell.RoomNeeds}|chair={shell.ChairTiles}|table={shell.TableTiles}|torch={shell.TorchTiles}|door={shell.DoorTiles}");
		}
	}

	private void CompleteBaseline(CommandCaller caller, HousingProbeRequest request, int presetCount, Rectangle envelope, HousingProbeCounters counters)
	{
		ClearProbeArea(envelope);
		string verdict = counters.Errors == 0 && (request.AllowsRegression || counters.Regression == 0) ? "BASELINE_PASS" : "FAIL";
		string summary = request.Mode switch
		{
			HousingProbeMode.FailedShells => $"{request.Prefix}_END|verdict={verdict}|withShells={counters.Pass}/{presetCount}|withoutShells={counters.Regression}/{presetCount}|errors={counters.Errors}",
			HousingProbeMode.All => $"{request.Prefix}_END|verdict={verdict}|actualPass={counters.Pass}/61|expectedRepair={counters.ExpectedRepair}/18|regressions={counters.Regression}|errors={counters.Errors}",
			_ => $"{request.Prefix}_END|verdict={verdict}|actualPass={counters.Pass}/{presetCount}|actualFail={counters.Regression}/{presetCount}|errors={counters.Errors}"
		};
		Mod.Logger.Info(summary);
		caller.Reply(summary, verdict == "BASELINE_PASS" ? Color.LightGreen : Color.OrangeRed);
	}
}
