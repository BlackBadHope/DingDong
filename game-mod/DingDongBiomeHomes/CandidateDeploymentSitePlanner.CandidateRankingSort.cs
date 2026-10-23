namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class RankedCandidateCollector
	{
		private void Sort()
		{
			// Tie-breakers keep the same lowest-score occupant and therefore a stable pool digest.
			Items.Sort(static (left, right) =>
			{
				int score = right.Score.CompareTo(left.Score);
				if (score != 0)
					return score;
				int x = left.Site.AssetArea.X.CompareTo(right.Site.AssetArea.X);
				if (x != 0)
					return x;
				int y = left.Site.AssetArea.Y.CompareTo(right.Site.AssetArea.Y);
				if (y != 0)
					return y;
				int width = left.Site.AssetArea.Width.CompareTo(right.Site.AssetArea.Width);
				if (width != 0)
					return width;
				int height = left.Site.AssetArea.Height.CompareTo(right.Site.AssetArea.Height);
				return height != 0 ? height : left.Site.Biome.CompareTo(right.Site.Biome);
			});
		}
	}
}
