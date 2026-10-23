using System;

namespace DingDongBiomeHomes;

public static partial class AuthoredResidenceLayoutPolicy
{
    private sealed partial class LayoutAcceptanceSession
    {
        private bool TryValidateHardSafety(out string failure)
        {
            if (spacing > Math.Max(0, maximumVisualSpacingViolations))
            {
                failure = $"visual-spacing-violations={spacing}";
                return false;
            }
            if (isolatedEnvelope > 0)
            {
                failure = $"isolated-envelope-violations={isolatedEnvelope}";
                return false;
            }
            if (landmarkMegaCell > 0)
            {
                failure = $"landmark-mega-sector-violations={landmarkMegaCell}";
                return false;
            }
            if (landmarkMegaStack > 0)
            {
                failure = $"landmark-mega-stack-violations={landmarkMegaStack}";
                return false;
            }
            if (highSkyCeiling > MaxHighSkySitesAtCeiling)
            {
                failure = $"high-sky-ceiling={highSkyCeiling}/{MaxHighSkySitesAtCeiling}";
                return false;
            }

            failure = string.Empty;
            return true;
        }
    }
}
