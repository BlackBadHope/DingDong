using System;
using System.Collections.Generic;
using System.Reflection;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen;

internal sealed class NativeHeightCompatibilitySystem : ModSystem
{
	private readonly List<ILHook> hooks = new();
	internal static bool Ready { get; private set; }
	public override void PostSetupContent()
	{
		try
		{
			foreach (var target in NativeHeightCompatibilityManifest.Targets)
			{
				Assembly assembly;
				if (target.Provider == "tModLoader") assembly = typeof(Main).Assembly;
				else if (ModLoader.TryGetMod(target.Provider, out Mod provider)) assembly = provider.Code;
				else continue;
				MethodBase method = assembly.ManifestModule.ResolveMethod(target.Token);
				if (method.Name != target.Name || method.DeclaringType?.FullName != target.Type)
					throw new MissingMethodException($"Audited height target changed: {target.Provider}/{target.Type}/{target.Name}");
				Func<int> layer = target.WallOfFlesh ? () => NativeUnderworldSystem.WallOfFleshLayer : () => NativeUnderworldSystem.Layer;
				Func<int, int> translate = target.WallOfFlesh ? NativeUnderworldSystem.TranslateWallOffset : NativeUnderworldSystem.TranslateBottomOffset;
				hooks.Add(new ILHook(method, il => NativeHeightCompatibilityPatches.Apply(il, target, layer, translate)));
			}
			int heightMethods=hooks.Count;
			foreach(var target in NativeCoastCompatibilityManifest.Targets)
			{
				Assembly assembly;
				if(target.Provider=="tModLoader")assembly=typeof(Main).Assembly;
				else if(ModLoader.TryGetMod(target.Provider,out Mod provider))assembly=provider.Code;
				else continue;
				MethodBase method=assembly.ManifestModule.ResolveMethod(target.Token);
				if(method.Name!=target.Name||method.DeclaringType?.FullName!=target.Type)throw new MissingMethodException("Audited coast target changed: "+target.Type+"/"+target.Name);
				hooks.Add(new ILHook(method,il=>NativeCoastCompatibilityPatches.Apply(il,target,NativeCoastalBiomeSystem.IsOcean,NativeCoastalBiomeSystem.Distance)));
			}
			Ready = true;
			Mod.Logger.Info($"DINGDONG_HEIGHT_COMPAT|status=ready|heightMethods={heightMethods}|coastMethods={hooks.Count-heightMethods}|untaggedUnderworldGeometryUnchanged=True");
			NativeHeightCompatibilityProbe.Run(Mod);
		}
		catch
		{
			Dispose();
			throw;
		}
	}
	public override void Unload() => Dispose();
	private void Dispose()
	{
		Ready = false;
		for (int i = hooks.Count - 1; i >= 0; i--) hooks[i].Dispose();
		hooks.Clear();
	}
}
