using Microsoft.Xna.Framework;

namespace DingDongBiomeHomes.Worldgen;

internal readonly record struct ContinentalDungeonPlan(
	Rectangle Room,
	Rectangle Connector,
	Rectangle Portal)
{
	internal bool HasPortal => Portal.Width > 0 && Portal.Height > 0;

	internal Rectangle ProtectedArea
	{
		get
		{
			Rectangle area = Rectangle.Union(Room, Connector);
			return HasPortal ? Rectangle.Union(area, Portal) : area;
		}
	}
}
