using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.GameContent.Biomes;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static partial class CalamityWorldgenAdapter
{
	private const string PlaceHash = "B56D62C5FFC4F9C99FB4669187B0CB5A43B2341199238E984E5AEF175A5ABD23";

	internal static void Install(Mod provider, List<IDisposable> hooks)
	{
		MethodInfo place = Exact66ProviderGate.Method(provider, "CalamityMod.World.SunkenSea",
			"Place", true, typeof(bool), typeof(Point));
		MethodInfo temple = Exact66ProviderGate.Method(provider, "CalamityMod.World.CustomTemple",
			"GenNewTemple", true, typeof(void), typeof(int), typeof(int));
		MethodInfo dungeonArchive = Exact66ProviderGate.Method(provider,
			"CalamityMod.World.DungeonArchive", "PlaceArchive", true, typeof(void));
		Type abyss = provider.Code.GetType("CalamityMod.World.Abyss", throwOnError: true);
		CalamityAbyssBufferScaler.Bind(abyss);
		hooks.Add(new Exact66ActionDisposable(CalamityAbyssBufferScaler.Reset));
		Exact66ProviderGate.AssertIl(place, PlaceHash);
		hooks.Add(new ILHook(place, PatchSunkenSeaScale));
		hooks.Add(new ILHook(temple, PatchCalamityTempleBudget));
		hooks.Add(new ILHook(dungeonArchive, PatchDungeonArchiveTileLookup));
		MethodInfo fullDesert = RequireVanillaPass("<AddGenPasses>b__288_18");
		hooks.Add(new ILHook(fullDesert, PatchFullDesertAttemptBudget));
		MethodInfo desertCleanup = typeof(DesertBiome).GetMethod("CleanupArea",
			BindingFlags.NonPublic | BindingFlags.Static, binder: null,
			types: [typeof(Rectangle)], modifiers: null)
			?? throw new MissingMethodException(typeof(DesertBiome).FullName, "CleanupArea");
		hooks.Add(new ILHook(desertCleanup, PatchFullDesertCleanup));
		MethodInfo mushroomPatches = RequireVanillaPass("<AddGenPasses>b__288_20");
		hooks.Add(new ILHook(mushroomPatches, PatchMushroomPatchesWorkBudget));
		IL_WorldGen.ShroomPatch += PatchMushroomRootSearch;
		hooks.Add(new Exact66ActionDisposable(() =>
			IL_WorldGen.ShroomPatch -= PatchMushroomRootSearch));
		MethodInfo jungleChests = RequireVanillaPass("<AddGenPasses>b__288_47");
		hooks.Add(new ILHook(jungleChests, PatchJungleChestAttempts));
		Type byRefInt = typeof(int).MakeByRefType();
		MethodInfo dungeonTraps = RequireVanillaMethod("MakeDungeon_Traps",
			byRefInt, typeof(int), byRefInt);
		MethodInfo dungeonLights = RequireVanillaMethod("MakeDungeon_Lights",
			typeof(ushort), byRefInt, typeof(int), byRefInt, typeof(int[]));
		MethodInfo dungeonGroundFurniture = RequireVanillaMethod(
			"MakeDungeon_GroundFurniture", typeof(int));
		hooks.Add(new ILHook(dungeonTraps, PatchDungeonTrapTarget));
		hooks.Add(new ILHook(dungeonLights, PatchDungeonLightTarget));
		hooks.Add(new ILHook(dungeonGroundFurniture,
			PatchDungeonGroundFurnitureAttempts));
		IL_WorldGen.makeTemple += PatchVanillaTempleBudget;
		hooks.Add(new Exact66ActionDisposable(() =>
			IL_WorldGen.makeTemple -= PatchVanillaTempleBudget));
		IL_WorldGen.MakeDungeon += PatchDungeonWorkBudget;
		hooks.Add(new Exact66ActionDisposable(() =>
			IL_WorldGen.MakeDungeon -= PatchDungeonWorkBudget));
		On_WorldGen.MakeDungeon += MakeDungeon;
		hooks.Add(new Exact66ActionDisposable(() =>
			On_WorldGen.MakeDungeon -= MakeDungeon));
	}

	private static MethodInfo RequireVanillaPass(string name)
	{
		Type closure = typeof(WorldGen).GetNestedType("<>c", BindingFlags.NonPublic)
			?? throw new MissingMemberException("Terraria.WorldGen", "<>c");
		return closure.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)
			?? throw new MissingMethodException(closure.FullName, name);
	}

	private static MethodInfo RequireVanillaMethod(string name, params Type[] signature) =>
		typeof(WorldGen).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static,
			binder: null, types: signature, modifiers: null)
		?? throw new MissingMethodException(typeof(WorldGen).FullName, name);

	private static void PatchSunkenSeaScale(ILContext il)
	{
		ILCursor cursor = new(il);
		if (!cursor.TryGotoNext(MoveType.After,
			i => i.MatchLdloc(2), i => i.MatchLdcR4(1f), i => i.MatchLdcR4(2f),
			i => i.MatchCall(typeof(MathHelper), nameof(MathHelper.Clamp))))
			throw new InvalidOperationException("Calamity Sunken Sea clamp IL pattern is missing.");
		cursor.Index--;
		cursor.EmitDelegate<Func<float, float>>(ContinentalAdapterMath.CalamitySunkenSeaMaximum);
	}
}
