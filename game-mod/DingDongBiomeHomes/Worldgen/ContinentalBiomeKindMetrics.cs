namespace DingDongBiomeHomes.Worldgen;

internal sealed class ContinentalBiomeKindMetrics
{
	internal const int MinimumCoveragePercent = 90;
	internal const int MinimumEligiblePerCoveredColumn = 16;
	internal int OwnedColumns { get; private set; }
	internal int CoveredColumns { get; private set; }
	internal long EligibleTiles { get; private set; }
	internal long AlreadyCorrectTiles { get; private set; }
	internal long ChangedTiles { get; private set; }
	internal int SkippedProtectedColumns { get; private set; }

	internal bool HasCoverage => TryValidate(out _);

	internal bool TryValidate(out string failure)
	{
		if (OwnedColumns <= 0 || CoveredColumns <= 0)
			failure = "empty-column-coverage";
		else if ((long)CoveredColumns + SkippedProtectedColumns != OwnedColumns)
			failure = "column-accounting-mismatch";
		else if ((long)CoveredColumns * 100L <
			(long)OwnedColumns * MinimumCoveragePercent)
			failure = "covered-below-90-percent";
		else if (EligibleTiles <
			(long)CoveredColumns * MinimumEligiblePerCoveredColumn)
			failure = "eligible-density-below-16";
		else if (AlreadyCorrectTiles + ChangedTiles != EligibleTiles)
			failure = "tile-accounting-mismatch";
		else
		{
			failure = string.Empty;
			return true;
		}
		return false;
	}

	internal void Own(int columns) => OwnedColumns += columns;
	internal void Cover(int columns) => CoveredColumns += columns;
	internal void SkipProtected(int columns) => SkippedProtectedColumns += columns;

	internal void Observe(bool alreadyCorrect)
	{
		EligibleTiles++;
		if (alreadyCorrect) AlreadyCorrectTiles++;
		else ChangedTiles++;
	}

	internal string Compact() =>
		$"coverage:{CoveredColumns}/{OwnedColumns}/" +
		$"eligibleDensity:{EligibleTiles}/{CoveredColumns}/" +
		$"correct:{AlreadyCorrectTiles}/changed:{ChangedTiles}/" +
		$"skippedProtected:{SkippedProtectedColumns}";
}
