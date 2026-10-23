using System;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes.Worldgen;

internal static class NativeHeightCompatibilityProbe
{
	internal static void Run(Mod mod)
	{
		if(!Main.dedServ||Environment.GetEnvironmentVariable("DINGDONG_HEIGHT_COMPAT_PROBE")!="1")return;
		int width=Main.maxTilesX,height=Main.maxTilesY,checks=0;
		var system=ModContent.GetInstance<NativeUnderworldSystem>();var original=new TagCompound();system.SaveWorldData(original);
		var coast=ModContent.GetInstance<NativeCoastalBiomeSystem>();var originalCoast=new TagCompound();coast.SaveWorldData(originalCoast);
		void Require(bool value,string name){if(!value)throw new InvalidOperationException(name);checks++;}
		try
		{
			Main.maxTilesX=28000;Main.maxTilesY=7200;
			Type closure=typeof(Terraria.ModLoader.Utilities.SpawnCondition).GetNestedType("<>c",BindingFlags.NonPublic);
			MethodInfo predicate=closure.GetMethod("<.cctor>b__112_94",BindingFlags.Instance|BindingFlags.NonPublic);
			object receiver=closure.GetField("<>9",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic).GetValue(null);
			foreach(int version in new[]{0,1,2})
			{
				system.LoadWorldData(new TagCompound{["nativeUnderworldGeometryVersion"]=version});
				int threshold=7200-NativeHeightBoundaryPolicy.Translate(190,7200,version);
				bool Hell(int y)=>(bool)predicate.Invoke(receiver,new object[]{new NPCSpawnInfo{SpawnTileY=y}});
				Require(Hell(threshold+1)&&!Hell(threshold)&&!Hell(threshold-1),"native Hell predicate boundary version "+version);
				Require(threshold==new[]{7010,6610,6810}[version],"saved geometry moved");
			}
			coast.LoadWorldData(new TagCompound{["CoastBoundsV1"]=new int[]{1200,2100,900,1800}});
			Require(NativeCoastalBiomeSystem.IsOcean(1100,2000),"left deep ocean missing");
			Require(!NativeCoastalBiomeSystem.IsOcean(1500,2000),"left inland treated as ocean");
			Require(NativeCoastalBiomeSystem.IsOcean(27900,1700)&&!NativeCoastalBiomeSystem.IsOcean(27900,1900),"right independent coast depth");
			using var bytes=new System.IO.MemoryStream();using(var writer=new System.IO.BinaryWriter(bytes,System.Text.Encoding.UTF8,true))coast.NetSend(writer);
			coast.ClearWorld();bytes.Position=0;using(var reader=new System.IO.BinaryReader(bytes,System.Text.Encoding.UTF8,true))coast.NetReceive(reader);
			Require(bytes.Position==bytes.Length&&NativeCoastalBiomeSystem.Left.Width==1200&&NativeCoastalBiomeSystem.Right.Width==900,"coast wire roundtrip");
		}
		finally
		{
			Main.maxTilesX=width;Main.maxTilesY=height;system.LoadWorldData(original);coast.LoadWorldData(originalCoast);
		}
		mod.Logger.Info($"DINGDONG_HEIGHT_RUNTIME_PROBE|verdict=PASS|checks={checks}|nativeHellPredicateExecuted=True|stateRestored=True|tilesAllocated=False|worldSaved=False|visualAcceptance=False");
	}
}
