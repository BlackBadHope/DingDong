using Microsoft.Xna.Framework;

namespace DingDongBiomeHomes;

public readonly struct HousingRoomInfo
{
	public Rectangle Bounds { get; }
	public Point CheckPoint { get; }
	public Point HomePoint { get; }

	public HousingRoomInfo(Rectangle bounds, Point checkPoint, Point homePoint)
	{
		Bounds = bounds;
		CheckPoint = checkPoint;
		HomePoint = homePoint;
	}
}
