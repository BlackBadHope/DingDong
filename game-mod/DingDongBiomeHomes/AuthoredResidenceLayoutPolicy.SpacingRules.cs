using System;
using Microsoft.Xna.Framework;

namespace DingDongBiomeHomes;

public static partial class AuthoredResidenceLayoutPolicy
{
	private static bool VerticallyOverlaps(Rectangle a, Rectangle b) => a.Top < b.Bottom && b.Top < a.Bottom;

	public static int HardVisualGap(int leftFocusWidth, int rightFocusWidth)
	{
		int extra = (int)(0.08 * Math.Max(leftFocusWidth, rightFocusWidth));
		return Math.Max(24, Math.Min(48, extra));
	}

	public static bool CanSharePortSettlement(AuthoredResidencePlacementContract left, AuthoredResidencePlacementContract right) => left.CanShareSettlement && right.CanShareSettlement && left.ClusterPolicy == ResidenceClusterPolicy.Port && right.ClusterPolicy == ResidenceClusterPolicy.Port;

	private static bool HardVisualTooClose(AuthoredResidencePlacementContract left, Rectangle leftFocus, AuthoredResidencePlacementContract right, Rectangle rightFocus)
	{
		if (CanSharePortSettlement(left, right))
			return false;
		int hardGap = HardVisualGap(leftFocus.Width, rightFocus.Width);
		return TooClose(leftFocus, rightFocus, hardGap, left.BroadLayer, right.BroadLayer);
	}

	private static int BaseGap(ResidenceScaleClass a, ResidenceScaleClass b)
	{
		ResidenceScaleClass larger = (int)a >= (int)b ? a : b;
		return larger switch
		{
			ResidenceScaleClass.Mega => 88,
			ResidenceScaleClass.Landmark => 64,
			ResidenceScaleClass.Large => 40,
			ResidenceScaleClass.Medium => 28,
			_ => 18
		};
	}

	private static bool TooClose(Rectangle a, Rectangle b, int required, BroadLayoutLayer layerA, BroadLayoutLayer layerB)
	{
		int vertical = Math.Max(24, required / 2);
		bool separatedH = a.Right + required <= b.Left || b.Right + required <= a.Left;
		bool separatedV = a.Bottom + vertical <= b.Top || b.Bottom + vertical <= a.Top;
		// Broad layers control density and cadence; valid padded Surface scenes
		// may still be separated vertically within the same broad layer.
		return !(separatedH || separatedV);
	}

	private static int AxisGap(Rectangle a, Rectangle b)
	{
		int horizontal = a.Right < b.Left ? b.Left - a.Right : b.Right < a.Left ? a.Left - b.Right : 0;
		int vertical = a.Bottom < b.Top ? b.Top - a.Bottom : b.Bottom < a.Top ? a.Top - b.Bottom : 0;
		if (horizontal == 0 && vertical == 0)
			return 0;
		if (horizontal == 0)
			return vertical;
		if (vertical == 0)
			return horizontal;
		return Math.Min(horizontal, vertical);
	}
}
