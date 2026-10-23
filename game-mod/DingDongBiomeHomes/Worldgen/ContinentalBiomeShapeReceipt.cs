namespace DingDongBiomeHomes.Worldgen;

internal sealed class ContinentalBiomeShapeReceipt
{
	internal ContinentalBiomeKindMetrics Sahara { get; } = new();
	internal ContinentalBiomeKindMetrics Arctic { get; } = new();
	internal ContinentalBiomeKindMetrics Amazon { get; } = new();
	internal long TotalChanged => Sahara.ChangedTiles + Arctic.ChangedTiles +
		Amazon.ChangedTiles;

	internal ContinentalBiomeKindMetrics For(ContinentalSurfaceKind kind) => kind switch
	{
		ContinentalSurfaceKind.Desert => Sahara,
		ContinentalSurfaceKind.Snow => Arctic,
		ContinentalSurfaceKind.Jungle => Amazon,
		_ => null
	};

	internal bool TryValidate(out string failure)
	{
		if (!Sahara.TryValidate(out string saharaFailure))
			failure = $"sahara-{saharaFailure}:{Sahara.Compact()}";
		else if (!Arctic.TryValidate(out string arcticFailure))
			failure = $"arctic-{arcticFailure}:{Arctic.Compact()}";
		else if (!Amazon.TryValidate(out string amazonFailure))
			failure = $"amazon-{amazonFailure}:{Amazon.Compact()}";
		else if (TotalChanged <= 0) failure = "zero-total-change";
		else
		{
			failure = string.Empty;
			return true;
		}
		return false;
	}

	internal string Compact() => $"sahara={Sahara.Compact()}|" +
		$"arctic={Arctic.Compact()}|amazon={Amazon.Compact()}|changed={TotalChanged}";
}
