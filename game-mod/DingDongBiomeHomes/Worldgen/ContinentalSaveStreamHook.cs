using System;
using System.IO;
using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen;

internal static partial class ContinentalSaveStreamHook
{
	private static ILHook tileSaveDataHook;
	private static ILHook wallSaveDataHook;
	private static ILHook tileLoadDataHook;
	private static ILHook wallLoadDataHook;

	internal static void Install()
	{
		Type tileIo = typeof(Mod).Assembly.GetType(
			"Terraria.ModLoader.IO.TileIO", throwOnError: true);
		BindingFlags nested = BindingFlags.Public | BindingFlags.NonPublic;
		MethodInfo tileSaveData = FindSaveData(
			tileIo.GetNestedType("TileIOImpl", nested));
		MethodInfo wallSaveData = FindSaveData(
			tileIo.GetNestedType("WallIOImpl", nested));
		MethodInfo tileLoadData = FindClosedMethod(
			tileIo.GetNestedType("TileIOImpl", nested), "LoadData", typeof(void));
		MethodInfo wallLoadData = FindClosedMethod(
			tileIo.GetNestedType("WallIOImpl", nested), "LoadData", typeof(void));
		try
		{
			tileSaveDataHook = new ILHook(tileSaveData, PatchSaveData);
			wallSaveDataHook = new ILHook(wallSaveData, PatchSaveData);
			tileLoadDataHook = new ILHook(tileLoadData, PatchLoadData);
			wallLoadDataHook = new ILHook(wallLoadData, PatchLoadData);
		}
		catch
		{
			Uninstall();
			throw;
		}
	}

	internal static void Uninstall()
	{
		wallLoadDataHook?.Dispose();
		tileLoadDataHook?.Dispose();
		wallSaveDataHook?.Dispose();
		tileSaveDataHook?.Dispose();
		wallLoadDataHook = null;
		tileLoadDataHook = null;
		wallSaveDataHook = null;
		tileSaveDataHook = null;
		activePlane = Plane.None;
	}

	private static MethodInfo FindSaveData(Type implementation) =>
		FindClosedMethod(implementation, "SaveData", typeof(byte[]));

	private static MethodInfo FindClosedMethod(Type implementation, string name,
		Type returnType)
	{
		MethodInfo method = implementation?.BaseType?.GetMethod(name,
			BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		if (method == null || method.ReturnType != returnType ||
			method.ContainsGenericParameters)
			throw new MissingMethodException(implementation?.FullName ?? "TileIO", name);
		return method;
	}

	private static void PatchSaveData(ILContext il)
	{
		ConstructorInfo emptyStream = typeof(MemoryStream).GetConstructor(Type.EmptyTypes);
		ConstructorInfo binaryWriter = typeof(BinaryWriter).GetConstructor(
			new[] { typeof(Stream) });
		MethodInfo toArray = typeof(MemoryStream).GetMethod(nameof(MemoryStream.ToArray),
			Type.EmptyTypes);
		ILCursor cursor = new(il);
		if (!cursor.TryGotoNext(MoveType.Before,
			instruction => instruction.MatchNewobj(emptyStream)))
			throw new InvalidOperationException("TileIO SaveData stream constructor changed.");
		cursor.Remove();
		cursor.Emit(OpCodes.Ldarg_0);
		cursor.EmitDelegate<Func<object, MemoryStream>>(CreatePlaneStream);

		cursor.Index = 0;
		if (!cursor.TryGotoNext(MoveType.Before,
			instruction => instruction.MatchNewobj(binaryWriter)))
			throw new InvalidOperationException("TileIO SaveData writer changed.");
		cursor.Remove();
		cursor.EmitDelegate<Func<Stream, BinaryWriter>>(CreatePlaneWriter);

		cursor.Index = 0;
		if (!cursor.TryGotoNext(MoveType.Before,
			instruction => instruction.MatchLdloc(0),
			instruction => instruction.MatchCallvirt(toArray)))
			throw new InvalidOperationException("TileIO SaveData final buffer changed.");
		cursor.Emit(OpCodes.Ldloc_1);
		cursor.EmitDelegate<Action<BinaryWriter>>(FinishPlaneWriter);

		cursor.Index = 0;
		if (!cursor.TryGotoNext(MoveType.Before,
			instruction => instruction.MatchCallvirt(toArray)))
			throw new InvalidOperationException("TileIO SaveData ToArray call changed.");
		cursor.Remove();
		cursor.EmitDelegate<Func<MemoryStream, byte[]>>(TakeEncodedBuffer);
	}

	private static void PatchLoadData(ILContext il)
	{
		MethodInfo getBytes = typeof(Terraria.ModLoader.IO.TagCompound).GetMethod(
			"GetByteArray", new[] { typeof(string) });
		ILCursor cursor = new(il);
		if (!cursor.TryGotoNext(MoveType.After,
			instruction => instruction.MatchCallvirt(getBytes)))
			throw new InvalidOperationException("TileIO LoadData byte array changed.");
		if (!cursor.Next.MatchLdcI4(0) || cursor.Next.Next?.Operand is not Mono.Cecil.MethodReference streamCall ||
			streamCall.Name != "ToMemoryStream" || streamCall.ReturnType.FullName != "System.IO.MemoryStream" ||
			cursor.Next.Next.Next?.Operand is not Mono.Cecil.MethodReference readerCtor ||
			readerCtor.DeclaringType.FullName != "System.IO.BinaryReader" || readerCtor.Name != ".ctor")
			throw new InvalidOperationException("TileIO LoadData sequential reader contract changed.");
		cursor.RemoveRange(2);
		cursor.EmitDelegate<Func<byte[], Stream>>(OpenPlane);
	}
}
