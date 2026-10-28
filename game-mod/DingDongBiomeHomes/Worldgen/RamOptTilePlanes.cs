using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen;

// RamOpt 28.10: the tile planes are every TileData<T> (tModLoader 1.4.4 TileData.cs),
// not only the five vanilla ones. In the 67-mod profile CalamityMod (TileSpecialDrawData)
// and androLib (TilePipeData) add 1 byte each (measured: 17 B per cell). Each plane
// subscribes to TileData.OnSetLength in its static ctor on first access and is then
// allocated at the current Tilemap length, so the admission counts every loaded
// ITileData struct, registered yet or not.
internal static class RamOptTilePlanes
{
	internal const int VanillaBytesPerCell = 15;
	private static int? allKnownBytesPerCell;
	private static string allKnownDetail;

	internal static int RegisteredBytesPerCell(out string detail)
	{
		try
		{
			List<Type> types = Registered();
			if (types == null)
			{
				detail = "reflection-unavailable";
				return VanillaBytesPerCell;
			}
			detail = Describe(types);
			return Math.Max(VanillaBytesPerCell, types.Sum(SizeOf));
		}
		catch (Exception ex) when (ex is TargetInvocationException or MemberAccessException or InvalidOperationException
			or NotSupportedException or ArgumentException)
		{
			detail = "reflection-failed:" + ex.GetType().Name;
			return VanillaBytesPerCell;
		}
	}

	// Registered planes plus every loaded ITileData struct that may register later.
	internal static int AllKnownBytesPerCell(out string detail)
	{
		if (allKnownBytesPerCell is int cached)
		{
			detail = allKnownDetail;
			return Math.Max(cached, RegisteredBytesPerCell(out _));
		}
		int registered = RegisteredBytesPerCell(out string registeredDetail);
		try
		{
			var types = new HashSet<Type>(Registered() ?? new List<Type>());
			var assemblies = new HashSet<Assembly> { typeof(ITileData).Assembly };
			foreach (Mod mod in ModLoader.Mods)
				if (mod.Code != null) assemblies.Add(mod.Code);
			foreach (Assembly assembly in assemblies)
				foreach (Type type in SafeTypes(assembly))
					if (type is { IsValueType: true, ContainsGenericParameters: false } &&
						typeof(ITileData).IsAssignableFrom(type))
						types.Add(type);
			allKnownBytesPerCell = Math.Max(registered, types.Sum(SizeOf));
			allKnownDetail = Describe(types);
		}
		catch (Exception ex) when (ex is TargetInvocationException or MemberAccessException or InvalidOperationException
			or NotSupportedException or ArgumentException)
		{
			allKnownBytesPerCell = registered;
			allKnownDetail = registeredDetail + ";scan-failed:" + ex.GetType().Name;
		}
		detail = allKnownDetail;
		return allKnownBytesPerCell.Value;
	}

	internal static void Reset()
	{
		allKnownBytesPerCell = null;
		allKnownDetail = null;
	}

	private static List<Type> Registered()
	{
		Type tileData = typeof(ITileData).Assembly.GetType("Terraria.TileData");
		FieldInfo field = tileData?.GetField("OnSetLength", BindingFlags.NonPublic | BindingFlags.Static);
		if (field == null || field.FieldType != typeof(Action<uint>))
			return null;
		var result = new List<Type>();
		// Measured 28.10 on the dedicated server: the field is still null after mod load,
		// because every plane (vanilla too) registers in TileData<T>'s static ctor on its
		// first access and is then allocated at the current length.
		if (field.GetValue(null) is not Action<uint> handlers)
			return result;
		foreach (Delegate handler in handlers.GetInvocationList())
		{
			Type owner = handler.Method.DeclaringType;
			if (owner is { IsGenericType: true } && owner.GetGenericTypeDefinition().FullName == "Terraria.TileData`1")
				result.Add(owner.GetGenericArguments()[0]);
		}
		return result;
	}

	private static IEnumerable<Type> SafeTypes(Assembly assembly)
	{
		try { return assembly.GetTypes(); }
		catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t != null); }
	}

	private static readonly MethodInfo SizeOfMethod = typeof(Unsafe).GetMethod(nameof(Unsafe.SizeOf));

	private static int SizeOf(Type type) => (int)SizeOfMethod.MakeGenericMethod(type).Invoke(null, null);

	private static string Describe(IEnumerable<Type> types) =>
		string.Join(",", types.Select(t => $"{t.FullName}:{SizeOf(t)}").OrderBy(s => s, StringComparer.Ordinal));
}
