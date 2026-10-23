using System;
using System.IO;
using DingDongBiomeHomes.Fishing.Worldgen;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes.Worldgen;

// Server surveys old worlds once; clients receive these same bounds, including
// asymmetrical coasts. Never infer a client coastline from unloaded sections.
internal sealed class NativeCoastalBiomeSystem : ModSystem
{
	internal static NativeCoastBand Left { get; private set; }
	internal static NativeCoastBand Right { get; private set; }
	internal static bool Ready { get; private set; }
	private static bool Enabled => ContinentalWorldDimensions.IsContinental && !Main.remixWorld && Ready;
	internal static int Distance(int native, bool right) => !Enabled ? native : (right ? Right : Left).Distance(native, WorldGen.beachDistance);
	internal static bool IsOcean(int x, int y)
	{
		if (!Enabled) return WorldGen.oceanDepths(x,y);
		var band = x < Main.maxTilesX / 2 ? Left : Right;
		if (band.Width == 0) return WorldGen.oceanDepths(x,y);
		return band.Contains(x < Main.maxTilesX / 2 ? x : Main.maxTilesX - 1 - x,y);
	}
	public override void ClearWorld() { Left=Right=default;Ready=false; }
	// OnWorldLoad runs before LoadWorldData and its result would be cleared by
	// our LoadWorldData hook. Wait for both saved water regions and geometry.
	public override void PostWorldLoad()
	{
		if (Main.netMode != NetmodeID.MultiplayerClient && ContinentalWorldDimensions.IsContinental && !Main.remixWorld)
		{
			CaptureGeneratedBounds();
			if (!Ready) { Left=LegacySurvey(false);Right=LegacySurvey(true);Ready=Left.Width>0 || Right.Width>0; }
			Mod.Logger.Info($"DINGDONG_COAST_BOUNDS|ready={Ready}|left={Left.Width},{Left.Bottom}|right={Right.Width},{Right.Bottom}|tilesModified=False");
		}
	}
	private static NativeCoastBand LegacySurvey(bool right)
	{
		NativeCoastBand observed = Survey(right);
		// Sparse water pockets or islands do not prove that the native beach
		// became narrower. Preserve the old predicate as a minimum, and include
		// Calamity's saved/generated footprint on its actual side of the world.
		int width = Math.Max(WorldGen.beachDistance, observed.Width);
		int bottom = Math.Max((int)Math.Ceiling(WorldGen.oceanLevel), observed.Bottom);
		if (FishingCalamityExpansion.TryLegacyCoast(right, out NativeCoastBand provider))
		{ width = Math.Max(width, provider.Width); bottom = Math.Max(bottom, provider.Bottom); }
		return new NativeCoastBand(Math.Clamp(width, 1, Main.maxTilesX / 4), Math.Clamp(bottom, 1, NativeUnderworldSystem.Layer - 1));
	}
	internal static void CaptureGeneratedBounds()
	{
		if (!ContinentalWorldDimensions.IsContinental) return;
		foreach (var region in FishingWaterWorldSystem.Regions)
		{
			if (region.Shape != FishingRegionShape.Ocean) continue;
			bool right=region.Bounds.Center.X >= Main.maxTilesX/2;
			int width=right ? Main.maxTilesX-region.Bounds.Left : region.Bounds.Right;
			var band=new NativeCoastBand(width,Math.Min(NativeUnderworldSystem.Layer-1,region.Bounds.Bottom));
			if (!band.Valid(Main.maxTilesX,Main.maxTilesY)) continue;
			if(right)Right=band;else Left=band;
		}
		Ready=Left.Width>0 || Right.Width>0;
	}
	private static NativeCoastBand Survey(bool right)
	{
		int X(int edge)=>right ? Main.maxTilesX-1-edge : edge;
		int ceiling=Math.Min((int)Main.rockLayer,NativeUnderworldSystem.Layer-32);
		int water=-1;
		foreach(int edge in new[]{64,128,256})
		{
			for(int y=32;y<ceiling-16;y++) if(Water(X(edge),y)&&Water(X(edge),y+8)){water=y;break;}
			if(water>=0)break;
		}
		if(water<0)return default;
		int last=0,dry=0,bottom=water;
		for(int edge=32;edge<Main.maxTilesX/4;edge+=16)
		{
			if(!Water(X(edge),water+8)){if(++dry>=4&&last>0)break;continue;}
			dry=0;last=edge;
			int y=water+8;
			while(y<NativeUnderworldSystem.Layer-32&&Water(X(edge),y))y++;
			bottom=Math.Max(bottom,y);
		}
		return last==0 ? default : new(Math.Min(Main.maxTilesX/4,last+32),Math.Min(NativeUnderworldSystem.Layer-1,bottom+8));
	}
	private static bool Water(int x,int y)
	{
		Tile tile=Main.tile[x,y];
		return tile.LiquidAmount>=128&&tile.LiquidType==LiquidID.Water&&(!tile.HasTile||!Main.tileSolid[tile.TileType]);
	}
	public override void SaveWorldData(TagCompound tag)
	{
		if(!Ready)return;
		tag["CoastBoundsV1"]=new int[]{Left.Width,Left.Bottom,Right.Width,Right.Bottom};
	}
	public override void LoadWorldData(TagCompound tag)
	{
		ClearWorld();
		int[] data=tag.GetIntArray("CoastBoundsV1");
		if(data.Length==0)return;
		if(data.Length!=4)throw new InvalidDataException("Invalid saved coast bounds.");
		Set(new(data[0],data[1]),new(data[2],data[3]));
	}
	private static void Set(NativeCoastBand left,NativeCoastBand right)
	{
		if(!left.Valid(Main.maxTilesX,Main.maxTilesY)||!right.Valid(Main.maxTilesX,Main.maxTilesY))throw new InvalidDataException("Coast bounds exceed world dimensions.");
		Left=left;Right=right;Ready=left.Width>0||right.Width>0;
	}
	public override void NetSend(BinaryWriter writer)
	{
		writer.Write((byte)1);writer.Write(Left.Width);writer.Write(Left.Bottom);writer.Write(Right.Width);writer.Write(Right.Bottom);
	}
	public override void NetReceive(BinaryReader reader)
	{
		if(reader.ReadByte()!=1)throw new InvalidDataException("Unknown coast bounds version.");
		var left=new NativeCoastBand(reader.ReadInt32(),reader.ReadInt32());
		var right=new NativeCoastBand(reader.ReadInt32(),reader.ReadInt32());
		Set(left,right);
	}
}
