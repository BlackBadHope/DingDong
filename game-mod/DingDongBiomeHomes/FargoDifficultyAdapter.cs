using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
internal sealed class FargoDifficultyAdapter : ReflectedDifficultyAdapter
{
	private readonly Func<bool> eternity;
	private readonly Func<bool> masochist;
	private readonly Func<bool> vanillaBehavior;
	internal FargoDifficultyAdapter(Mod mod) : base(mod, "Fargo's Souls")
	{
		Type worldType = RequireType(mod, "FargowiltasSouls.Core.Systems.WorldSavingSystem");
		eternity = BindStaticBool(worldType, "EternityMode");
		masochist = BindStaticBool(worldType, "MasochistModeReal");
		vanillaBehavior = BindStaticBool(worldType, "EternityVanillaBehaviour");
	}

	public override DamageDifficultyEntry Read(bool worldReady)
	{
		if (!worldReady)
			return WaitingForWorld();
		try
		{
			bool eternityActive = eternity();
			bool masochistActive = masochist();
			bool vanillaBehaviorActive = vanillaBehavior();
			string mode = masochistActive ? "Masochist" : eternityActive ? "Eternity" : Text("NoCustomMode");
			string detail = Text("FargoDetail", vanillaBehaviorActive ? Text("Enabled") : Text("Disabled"));
			return new DamageDifficultyEntry(Source, mode, detail, AdapterHealthy: true);
		}
		catch (Exception exception)
		{
			return Failed(exception);
		}
	}
}
