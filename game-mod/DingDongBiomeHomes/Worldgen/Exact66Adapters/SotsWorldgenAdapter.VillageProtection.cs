using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using MonoMod.RuntimeDetour;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static partial class SotsWorldgenAdapter
{
	private static FieldInfo villageOuterField, villageRectField;
	private static FieldInfo villageGrassField, villageStoneField;
	private static Func<Rectangle, int, bool> villageNativeValid;
	private delegate bool SelectVillage(ref int x, ref int y, int width, int height);

	private static void InstallVillageProtection(Mod provider, List<IDisposable> hooks)
	{
		VerifyVillageFootprints(provider);
		MethodInfo designate = Exact66ProviderGate.Method(provider, AbandonedVillageType,
			"DesignateAVRectangle", true, typeof(void), typeof(int), typeof(int), typeof(int), typeof(int));
		MethodInfo ambient = Exact66ProviderGate.Method(provider, AbandonedVillageType,
			"TryPlacingAmbientTiles", true, typeof(void), typeof(int), typeof(int), typeof(bool));
		MethodInfo prepare = Exact66ProviderGate.Method(provider, AbandonedVillageType,
			"PrepareUnderground", true, typeof(void), typeof(Rectangle), typeof(float), typeof(float), typeof(int));
		MethodInfo descent = Exact66ProviderGate.Method(provider, AbandonedVillageType,
			"GenerateDownwardPath", true, typeof(void), typeof(int), typeof(int));
		MethodInfo valid = Exact66ProviderGate.Method(provider, AbandonedVillageType,
			"ValidLocation", true, typeof(bool), typeof(Rectangle), typeof(int));
		Exact66ProviderGate.AssertIl(designate, "87228E3ADA855D7D2F467B4D24E15EDFA55BAC4FA49194E6A0F7AF41DF9DE2E9");
		Exact66ProviderGate.AssertIl(ambient, "80FC9B25D3B13396FD5F54D76EA375B31F56CC4991E12C4FB87515ED7346548C");
		Exact66ProviderGate.AssertIl(prepare, "9BA78BAB01F0C485ED6EA91A5DC398B278395ED3118E9C8C9D8C1C6FFC544D07");
		Exact66ProviderGate.AssertIl(descent, "28C17DBF10AC4A45E6BA7002E6B90FD432D249A849EEE29C4218F6E6421288AD");
		Exact66ProviderGate.AssertIl(valid, "F2C483ADA401712FDD3369C1E327A49C59D1295938F6AA1561256E643C5E825B");
		Type type = designate.DeclaringType;
		villageOuterField = type.GetField("OuterRect", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
			?? throw new MissingFieldException(AbandonedVillageType, "OuterRect");
		villageRectField = type.GetField("AVRect", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
			?? throw new MissingFieldException(AbandonedVillageType, "AVRect");
		villageGrassField = type.GetField("ValidGrassTiles", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
			?? throw new MissingFieldException(AbandonedVillageType, "ValidGrassTiles");
		villageStoneField = type.GetField("ValidStoneTiles", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
			?? throw new MissingFieldException(AbandonedVillageType, "ValidStoneTiles");
		villageNativeValid = (Func<Rectangle, int, bool>)valid.CreateDelegate(typeof(Func<Rectangle, int, bool>));
		hooks.Add(new ILHook(designate, PatchVillageCandidate));
		hooks.Add(new ILHook(ambient, PatchVillageAmbient));
		hooks.Add(new ILHook(prepare, PatchVillageConversion));
		hooks.Add(new ILHook(descent, PatchVillageDescent));
	}
}
