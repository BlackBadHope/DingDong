using System;
using System.Reflection;
using System.Security.Cryptography;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static class Exact66ProviderGate
{
	internal static Mod Require(string name, Version version, string mvid)
	{
		if (!ModLoader.TryGetMod(name, out Mod mod) || mod.Code is null)
			throw new InvalidOperationException($"Exact66 worldgen provider {name} is unavailable.");
		Guid actual = mod.Code.ManifestModule.ModuleVersionId;
		if (mod.Version != version || actual != Guid.Parse(mvid))
			throw new InvalidOperationException(
				$"Unsupported {name}: {mod.Version}/{actual}; expected {version}/{mvid}.");
		return mod;
	}

	internal static MethodInfo Method(Mod mod, string typeName, string methodName,
		bool isStatic, Type result, params Type[] parameters)
	{
		Type type = mod.Code.GetType(typeName, throwOnError: true);
		BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
			(isStatic ? BindingFlags.Static : BindingFlags.Instance);
		MethodInfo method = type.GetMethod(methodName, flags, null, parameters, null);
		if (method is null || method.IsStatic != isStatic || method.ReturnType != result)
			throw new MissingMethodException(typeName, methodName);
		return method;
	}

	internal static void AssertIl(MethodInfo method, string expected)
	{
		byte[] il = method.GetMethodBody()?.GetILAsByteArray() ??
			throw new InvalidOperationException($"{method.DeclaringType?.FullName}::{method.Name} has no IL body.");
		string actual = Convert.ToHexString(SHA256.HashData(il));
		if (!actual.Equals(expected, StringComparison.Ordinal))
			throw new InvalidOperationException(
				$"IL fingerprint changed for {method.DeclaringType?.FullName}::{method.Name}: {actual}.");
	}
}
