using System;

namespace DingDongBiomeHomes;

internal enum HousingProbeMode
{
	All,
	Reserve,
	FailedShells,
	Replacements,
	Showcase,
	Intake,
	RoadmapR6,
	RoadmapR7,
	Gallery
}

internal readonly record struct HousingProbeRequest(HousingProbeMode Mode, string WorldFile)
{
	internal string Prefix => Mode switch
	{
		HousingProbeMode.RoadmapR7 => "DINGDONG_ROADMAP_R7_HOUSING_PROBE",
		HousingProbeMode.RoadmapR6 => "DINGDONG_ROADMAP_R6_HOUSING_PROBE",
		HousingProbeMode.Intake => "DINGDONG_RESIDENCE_INTAKE_HOUSING_PROBE",
		HousingProbeMode.Showcase => "DINGDONG_SHOWCASE_HOUSING_PROBE",
		HousingProbeMode.Replacements => "DINGDONG_REPLACEMENT_HOUSING_PROBE",
		HousingProbeMode.Reserve => "DINGDONG_RESERVE_HOUSING_PROBE",
		HousingProbeMode.FailedShells => "DINGDONG_FAILED_SHELL_PROBE",
		_ => "DINGDONG_HOUSING_PROBE"
	};

	internal bool AllowsRegression => Mode != HousingProbeMode.All;
	internal bool NeedsSupplementCheck => Mode == HousingProbeMode.All;

	internal static bool TryParse(string value, out HousingProbeMode mode)
	{
		mode = value.ToLowerInvariant() switch
		{
			"all" => HousingProbeMode.All,
			"reserve" => HousingProbeMode.Reserve,
			"failed-shells" => HousingProbeMode.FailedShells,
			"replacements" => HousingProbeMode.Replacements,
			"showcase" => HousingProbeMode.Showcase,
			"intake" => HousingProbeMode.Intake,
			"roadmap-r6" => HousingProbeMode.RoadmapR6,
			"roadmap-r7" => HousingProbeMode.RoadmapR7,
			"gallery" => HousingProbeMode.Gallery,
			_ => (HousingProbeMode)(-1)
		};
		return (int)mode >= 0;
	}
}

internal sealed class HousingProbeCounters
{
	internal int Pass;
	internal int ExpectedRepair;
	internal int Regression;
	internal int Errors;
}
