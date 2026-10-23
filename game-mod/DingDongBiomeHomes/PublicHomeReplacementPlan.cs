using System;

namespace DingDongBiomeHomes;

// Pure owned geometry: no Terraria world, RNG, filesystem, private payload or image dependency.
public enum PublicHomeTile : byte { Preserve, Air, Block, Platform, ClosedDoor, Chair, Table, Torch, Glass }
public readonly record struct PublicHomeCell(PublicHomeTile Tile, bool SafeWall, short FrameX = 0, short FrameY = 0);
public readonly record struct PublicHomeRoom(int X, int Y, int Width, int Height);

public sealed class PublicHomeReplacementPlan
{
	public int Width { get; }
	public int Height { get; }
	public int GroundY { get; }
	public int Palette { get; }
	public PublicHomeCell[] Cells { get; }
	public PublicHomeRoom[] Rooms { get; }
	private const int RoomWidth = 16, RoomHeight = 8, RowPitch = 10;

	public static void ValidateDimensions(int width, int height)
	{
		if (width < 20 || height < 20 || width > 4096 || height > 4096)
			throw new ArgumentOutOfRangeException(nameof(width), $"Unsupported public home canvas: {width}x{height}.");
	}

	public static int RoomCount(string id, int width, int height)
	{
		ValidateDimensions(width, height);
		int geometricCapacity = Columns(width) * Math.Max(1, (height - 4) / RowPitch);
		int requested = PublicHomeSlotContract.Required(id, Math.Min(8, geometricCapacity));
		if (requested < 1 || requested > geometricCapacity)
			throw new InvalidOperationException($"Public home slot contract does not fit canvas: {id}.");
		return requested;
	}

	private static int Columns(int width) => Math.Clamp((width - 4) / (RoomWidth + 2), 1, 6);

	public PublicHomeReplacementPlan(string id, int width, int height, AuthoredPlacementMode mode)
	{
		ValidateDimensions(width, height);
		Width = width; Height = height;
		uint seed = Hash(id);
		Palette = (int)(seed % 4);
		int count = RoomCount(id, width, height), columns = Math.Min(Columns(width), count);
		int rows = (count + columns - 1) / columns;
		int proposedGround = mode switch
		{
			AuthoredPlacementMode.SurfaceStructure => height - 4,
			AuthoredPlacementMode.HarborScene => height / 2,
			AuthoredPlacementMode.FloatingScene => height * 2 / 3,
			AuthoredPlacementMode.UndergroundPoiScene => height * 3 / 4,
			_ => height * 3 / 5
		};
		GroundY = Math.Clamp(proposedGround, (rows - 1) * RowPitch + RoomHeight + 2, height - 4);
		Cells = new PublicHomeCell[checked(width * height)];
		Rooms = new PublicHomeRoom[count];
		int margin = 2;
		// A terrace and narrow piers give each independent room a shared walkway.
		// Empty canvas cells remain Preserve; this never clears an entire source-sized rectangle.
		for (int row = 0; row < rows; row++)
		{
			int floor = GroundY - row * RowPitch;
			for (int x = margin; x < width - margin; x++) Put(x, floor, PublicHomeTile.Platform);
		}
		int room = 0;
		for (int row = 0; row < rows; row++)
		for (int column = 0; column < columns && room < count; column++)
		{
			int x = columns == 1 ? (width - RoomWidth) / 2 :
				margin + (width - 2 * margin - RoomWidth) * column / (columns - 1);
			int floor = GroundY - row * RowPitch;
			int top = floor - RoomHeight + 1;
			Rooms[room++] = new PublicHomeRoom(x, top, RoomWidth, RoomHeight);
			DrawRoom(x, top, seed + (uint)room);
			if (row == 0)
				for (int y = floor + 1; y < height - 2; y++)
				{
					Put(x + 1, y, PublicHomeTile.Block);
					Put(x + RoomWidth - 2, y, PublicHomeTile.Block);
				}
		}
		// Own skyline accents. Their proportions are generated from this canvas,
		// not sampled from the old author-labelled IDs or any source structure.
		if (width >= 90 && height >= 70)
		{
			int tower = Math.Max(2, GroundY - Math.Min(height / 3, 64));
			for (int y = tower; y < GroundY; y++)
				if (!Cells[width / 2 * Height + y].SafeWall) Put(width / 2, y, PublicHomeTile.Block);
			for (int x = width / 2 - 2; x <= width / 2 + 2; x++)
				if (!Cells[x * Height + tower].SafeWall) Put(x, tower, PublicHomeTile.Block);
			if (!Cells[width / 2 * Height + tower - 1].SafeWall) Put(width / 2, tower - 1, PublicHomeTile.Torch);
		}
	}

	private void DrawRoom(int left, int top, uint variation)
	{
		int floor = top + RoomHeight - 1, right = left + RoomWidth - 1;
		for (int x = left; x <= right; x++)
		for (int y = top; y <= floor; y++)
		{
			bool boundary = x == left || x == right || y == top || y == floor;
			Put(x, y, boundary ? PublicHomeTile.Block : PublicHomeTile.Air, wall: true);
		}
		for (int y = 0; y < 3; y++) Put(left, floor - 3 + y, PublicHomeTile.ClosedDoor, true, 0, (short)(y * 18));
		// Complete vanilla style-0 furniture frames, constructed from dimensions only.
		for (int x = 0; x < 3; x++)
		for (int y = 0; y < 2; y++) Put(left + 3 + x, floor - 2 + y, PublicHomeTile.Table, true, (short)(x * 18), (short)(y * 18));
		for (int y = 0; y < 2; y++) Put(left + 9, floor - 2 + y, PublicHomeTile.Chair, true, 0, (short)(y * 18));
		Put(left + 6, top + 2, PublicHomeTile.Torch, true);
		// Closed glass window: no hole in either room boundary or background wall.
		for (int y = top + 2; y <= top + 3; y++) Put(right, y, PublicHomeTile.Glass, true);
		if ((variation & 1) == 0)
			for (int x = left + 1; x < right; x++) Put(x, top - 1, PublicHomeTile.Platform);
	}

	private void Put(int x, int y, PublicHomeTile tile, bool wall = false, short frameX = 0, short frameY = 0)
	{
		if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
			throw new InvalidOperationException("Public home plan exceeded its own canvas.");
		Cells[x * Height + y] = new PublicHomeCell(tile, wall, frameX, frameY);
	}

	private static uint Hash(string text)
	{
		if (string.IsNullOrEmpty(text)) throw new ArgumentException("A public home needs an identifier.", nameof(text));
		uint result = 2166136261;
		foreach (char value in text) result = unchecked((result ^ value) * 16777619);
		return result;
	}
}
