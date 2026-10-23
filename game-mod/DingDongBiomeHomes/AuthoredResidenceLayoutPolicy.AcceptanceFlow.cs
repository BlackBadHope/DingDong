namespace DingDongBiomeHomes;

public static partial class AuthoredResidenceLayoutPolicy
{
    private sealed partial class LayoutAcceptanceSession
    {
        public bool TryAccept(out string failure, out string report)
        {
            if (!TryCollectSites(out failure))
            {
                report = string.Empty;
                return false;
            }

            EvaluateSectorCapacity();
            EvaluateDistribution();
            report = BuildReport();
            if (!TryValidateHardSafety(out failure))
                return false;
            return TryValidateDistribution(out failure);
        }
    }
}
