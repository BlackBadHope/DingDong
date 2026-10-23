using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Microsoft.Xna.Framework;

namespace DingDongBiomeHomes.Worldgen;

internal sealed class NativeUnderworldSystem : ModSystem
{
	private readonly List<ILHook> hooks = new();
	private static bool installed, armed;
	private static int geometryVersion;
	private static int wallOfFleshLayer = -1;
	internal static int GeometryVersion => geometryVersion;
	private const string Tag = "nativeUnderworldGeometryVersion";
	internal static bool Active => ContinentalWorldDimensions.IsContinental &&
		(geometryVersion > 0 || armed && WorldGen.gen);
	internal static int ScaleOffset(int native) =>
		NativeUnderworldPolicy.SavedOffset(native, Main.maxTilesY,
			Active ? (armed ? NativeUnderworldPolicy.CurrentVersion : geometryVersion) : 0);
	internal static int Layer => Main.maxTilesY - ScaleOffset(200);
	internal static int WallOfFleshLayer => Active && wallOfFleshLayer >= Layer ? wallOfFleshLayer : Layer;
	internal static int RuntimeGeometryVersion => Active ? (armed ? NativeUnderworldPolicy.CurrentVersion : geometryVersion) : 0;
	internal static int TranslateBottomOffset(int native) => NativeHeightBoundaryPolicy.Translate(native, Main.maxTilesY, RuntimeGeometryVersion);
	internal static int TranslateWallOffset(int native) => Active ? checked(Main.maxTilesY - WallOfFleshLayer + native - 200) : native;
	internal static int ScaleDepthDraw(int native, int baseline) =>
		NativeUnderworldPolicy.DepthDraw(native, baseline, Main.maxTilesY, Active);

	public override void Load()
	{
		try
		{
			var closure = typeof(WorldGen).GetNestedType("<>c", BindingFlags.NonPublic);
			var terrain = closure?.GetMethod("<AddGenPasses>b__288_27", BindingFlags.NonPublic | BindingFlags.Instance)
				?? throw new MissingMethodException("Native Underworld pass");
			hooks.Add(new ILHook(terrain, NativeUnderworldPatches.Terrain));
			hooks.Add(new ILHook(typeof(WorldGen).GetMethod(nameof(WorldGen.AddHellHouses)),
				il => NativeUnderworldPatches.BottomOffsets(il, 300)));
			hooks.Add(new ILHook(typeof(Main).GetProperty(nameof(Main.UnderworldLayer)).GetMethod,
				il => NativeUnderworldPatches.BottomOffsets(il, 200)));
			NativeUnderworldLayerConsumers.Install(hooks);
			NativeSanctuaryNativeAdmission.Install(hooks);
			On_NPC.SpawnWOF += PrepareWallOfFleshSpawn;
			installed = true;
			Mod.Logger.Info($"DINGDONG_UNDERWORLD|hooks=ready|geometryVersion={NativeUnderworldPolicy.CurrentVersion}|nativeConsumers={NativeUnderworldLayerConsumers.Manifest.Length}");
		}
		catch (Exception error)
		{
			DisposeHooks();
			Mod.Logger.Warn("DINGDONG_UNDERWORLD|hooks=unsupported|" + error);
		}
	}

	private static void PrepareWallOfFleshSpawn(On_NPC.orig_SpawnWOF orig, Vector2 position)
	{
		ObserveWallOfFlesh(position.Y);
		orig(position);
	}

	internal static void ObserveWallOfFlesh(float centerY)
	{
		if (!Active) return;
		int centerTile = (int)(centerY / 16f);
		wallOfFleshLayer = Math.Clamp(centerTile - 45, Layer, Main.maxTilesY - 90);
	}

	public override void PostUpdateNPCs()
	{
		if (wallOfFleshLayer >= 0 && (Main.wofNPCIndex < 0 || Main.wofNPCIndex >= Main.maxNPCs || !Main.npc[Main.wofNPCIndex].active))
			wallOfFleshLayer = -1;
	}

	internal static void Arm()
	{
		if (!installed) throw new InvalidOperationException("Cannot generate x10: Underworld hooks unavailable.");
		armed = true;
		geometryVersion = 0;
	}
	internal static void Generated() { if (armed) geometryVersion = NativeUnderworldPolicy.CurrentVersion; }
	public override void ClearWorld() { armed = false; geometryVersion = 0; wallOfFleshLayer = -1; }
	public override void PreWorldGen() => ClearWorld();
	public override void SaveWorldData(TagCompound tag) { if (geometryVersion > 0) tag[Tag] = geometryVersion; }
	public override void LoadWorldData(TagCompound tag)
	{
		armed = false;
		SetVersion(tag.GetInt(Tag));
	}
	public override void NetSend(BinaryWriter writer) => writer.Write((byte)geometryVersion);
	public override void NetReceive(BinaryReader reader)
	{
		armed = false;
		SetVersion(reader.ReadByte());
	}
	private static void SetVersion(int version)
	{
		if (!NativeUnderworldPolicy.IsSupportedVersion(version))
			throw new InvalidDataException("Unknown Underworld geometry version: " + version);
		if (version > 0 && !installed)
			throw new InvalidOperationException("This world requires its expanded Underworld hooks.");
		geometryVersion = version;
	}
	public override void Unload() { On_NPC.SpawnWOF -= PrepareWallOfFleshSpawn; ClearWorld(); DisposeHooks(); }
	private void DisposeHooks()
	{
		for (int i = hooks.Count - 1; i >= 0; i--) hooks[i].Dispose();
		hooks.Clear();
		installed = false;
	}
}
