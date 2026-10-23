using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
internal abstract class ReflectedDifficultyAdapter : IOptionalDifficultyAdapter
{
	protected ReflectedDifficultyAdapter(Mod mod, string source)
	{
		Mod = mod;
		Source = source;
	}

	protected Mod Mod { get; }
	protected string Source { get; }

	public abstract DamageDifficultyEntry Read(bool worldReady);
	protected DamageDifficultyEntry WaitingForWorld() => new(Source, Text("WorldNotLoaded"), Text("WorldNotLoadedDetail"), AdapterHealthy: true);
	protected DamageDifficultyEntry Failed(Exception exception) => new(Source, Text("CompatibilityUnavailable"), $"{exception.GetType().Name}: {exception.Message}", AdapterHealthy: false);
	protected static Type RequireType(Mod mod, string fullName) => mod.Code.GetType(fullName, throwOnError: true, ignoreCase: false);
	protected static Func<bool> BindStaticBool(Type type, string name)
	{
		const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
		FieldInfo field = type.GetField(name, flags);
		if (field?.FieldType == typeof(bool))
			return () => (bool)field.GetValue(null);
		PropertyInfo property = type.GetProperty(name, flags);
		if (property?.PropertyType == typeof(bool) && property.GetMethod?.IsStatic == true)
			return () => (bool)property.GetValue(null);
		throw new MissingMemberException(type.FullName, name);
	}

	protected static Func<object> BindStaticProperty(Type type, string name)
	{
		const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
		PropertyInfo property = type.GetProperty(name, flags);
		if (property?.GetMethod?.IsStatic != true)
			throw new MissingMemberException(type.FullName, name);
		return () => property.GetValue(null);
	}

	protected static Func<object, object> BindInstanceProperty(Type type, string name)
	{
		const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
		PropertyInfo[] properties = type.GetProperties(flags);
		PropertyInfo property = Array.Find(properties, candidate => candidate.Name == name && candidate.DeclaringType == type && candidate.GetMethod != null && !candidate.GetMethod.IsStatic && candidate.GetIndexParameters().Length == 0) ?? Array.Find(properties, candidate => candidate.Name == name && candidate.GetMethod != null && !candidate.GetMethod.IsStatic && candidate.GetIndexParameters().Length == 0);
		if (property == null)
			throw new MissingMemberException(type.FullName, name);
		return instance => property.GetValue(instance);
	}

	protected static string DisplayValue(object value)
	{
		if (value is LocalizedText localized)
			return localized.Value;
		return Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;
	}

	protected static string Text(string key, params object[] args) => Language.GetTextValue($"Mods.DingDongBiomeHomes.Damage.Guide.Difficulty.{key}", args);
}
