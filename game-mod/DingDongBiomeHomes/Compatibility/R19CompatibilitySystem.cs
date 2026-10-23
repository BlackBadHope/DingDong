using System;
using System.Collections.Generic;
using System.Reflection;
using MonoMod.RuntimeDetour;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Compatibility;

public sealed class R19CompatibilitySystem : ModSystem
{
	private readonly List<ILHook> hooks = new();

	public override void Load()
	{
		try
		{
			SignatureProjectileCompatibility.Install(hooks);
			CalamityGravityCompatibility.Install(hooks);
			MagicStorageResultCompatibility.Install(hooks);
			CalamityVanitiesCompatibility.Install(hooks);
			FargoSummonCompatibility.Install(hooks);
			DanmakuProjectileCompatibility.Install(hooks);
			FargoMoneyTroughCompatibility.Install(hooks);
			LansUncraftCompatibility.Install(hooks);
			LuiafkBuildingCompatibility.Install(hooks);
			AndroStorageNetwork.Install(hooks);
		}
		catch { Unload(); throw; }
	}

	public override void Unload()
	{
		for (int i = hooks.Count - 1; i >= 0; i--) hooks[i].Dispose();
		hooks.Clear();
		SignatureProjectileCompatibility.Reset();
		DanmakuProjectileCompatibility.Reset();
		LansUncraftCompatibility.Reset();
		LuiafkBuildingCompatibility.Reset();
		AndroStorageNetwork.Reset();
	}

	internal static Type Provider(string name, string version, string type)
	{
		if (!ModLoader.TryGetMod(name, out Mod mod)) return null;
		if (mod.Version != Version.Parse(version))
		{
			DingDongBiomeHomes.Instance.Logger.Warn($"DINGDONG_COMPAT|provider={name}|version={mod.Version}|status=UNSUPPORTED|expected={version}");
			return null;
		}
		return mod.Code.GetType(type, throwOnError: true);
	}

	internal static MethodInfo Method(Type type, string name) =>
		type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
		?? throw new MissingMethodException(type.FullName, name);
}
