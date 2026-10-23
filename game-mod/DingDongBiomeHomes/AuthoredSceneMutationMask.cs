using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
public sealed partial class AuthoredSceneMutationMask
{
	private readonly bool[] mutationCells;
	private readonly Point[] exteriorContourCells;
	public int Width { get; }
	public int Height { get; }
	public int MutationCellCount { get; }
	public Rectangle MutationBounds { get; }
	public int? TerrainAnchorY { get; }
	public Rectangle? FocusBounds { get; }
	public int DiscardedMutationCellCount { get; }

	internal AuthoredSceneMutationMask(int width, int height, bool[] mutationCells, int mutationCellCount, Rectangle mutationBounds, int? terrainAnchorY, Rectangle? focusBounds, int discardedMutationCellCount)
	{
		Width = width;
		Height = height;
		this.mutationCells = mutationCells;
		exteriorContourCells = BuildExteriorContourCells(width, height, mutationCells);
		MutationCellCount = mutationCellCount;
		MutationBounds = mutationBounds;
		TerrainAnchorY = terrainAnchorY;
		FocusBounds = focusBounds;
		DiscardedMutationCellCount = discardedMutationCellCount;
	}

	public Rectangle ProtectedFootprint(Point origin, int padding) => new(origin.X + MutationBounds.X - padding, origin.Y + MutationBounds.Y - padding, MutationBounds.Width + padding * 2, MutationBounds.Height + padding * 2);
	public Rectangle VisualFootprint(Point origin, int padding)
	{
		Rectangle bounds = FocusBounds ?? MutationBounds;
		return new Rectangle(origin.X + bounds.X - padding, origin.Y + bounds.Y - padding, bounds.Width + padding * 2, bounds.Height + padding * 2);
	}

	public bool MutatesLocal(int x, int y)
	{
		if (x < 0 || y < 0 || x >= Width || y >= Height)
			return false;
		return mutationCells[x * Height + y];
	}

	public IReadOnlyList<Point> ExteriorContourCells => exteriorContourCells;

	public bool IntersectsTranslated(Point origin, AuthoredSceneMutationMask other, Point otherOrigin)
	{
		Rectangle area = new(origin.X, origin.Y, Width, Height);
		Rectangle otherArea = new(otherOrigin.X, otherOrigin.Y, other.Width, other.Height);
		Rectangle overlap = Rectangle.Intersect(area, otherArea);
		if (overlap.Width <= 0 || overlap.Height <= 0)
			return false;
		for (int worldX = overlap.Left; worldX < overlap.Right; worldX++)
		{
			int localX = worldX - origin.X;
			int otherX = worldX - otherOrigin.X;
			for (int worldY = overlap.Top; worldY < overlap.Bottom; worldY++)
			{
				int localIndex = localX * Height + worldY - origin.Y;
				int otherIndex = otherX * other.Height + worldY - otherOrigin.Y;
				if (mutationCells[localIndex] && other.mutationCells[otherIndex])
					return true;
			}
		}

		return false;
	}
}
