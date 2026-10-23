using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
internal sealed class CalamityDifficultyAdapter : ReflectedDifficultyAdapter
{
	private readonly Func<bool> revengeance;
	private readonly Func<bool> death;
	private readonly Func<bool> legendary;
	private readonly Func<bool> malice;
	private readonly Func<object> currentDifficulty;
	private readonly Func<object, object> difficultyName;
	private readonly Func<object, object> difficultyScale;
	internal CalamityDifficultyAdapter(Mod mod) : base(mod, "Calamity")
	{
		Type worldType = RequireType(mod, "CalamityMod.World.CalamityWorld");
		Type difficultySystemType = RequireType(mod, "CalamityMod.Systems.DifficultyModeSystem");
		Type difficultyModeType = RequireType(mod, "CalamityMod.Systems.DifficultyMode");
		revengeance = BindStaticBool(worldType, "revenge");
		death = BindStaticBool(worldType, "death");
		legendary = BindStaticBool(worldType, "LegendaryMode");
		malice = BindStaticBool(worldType, "MaliceMode");
		currentDifficulty = BindStaticProperty(difficultySystemType, "GetCurrentDifficulty");
		difficultyName = BindInstanceProperty(difficultyModeType, "Name");
		difficultyScale = BindInstanceProperty(difficultyModeType, "DifficultyScale");
	}

	public override DamageDifficultyEntry Read(bool worldReady)
	{
		if (!worldReady)
			return WaitingForWorld();
		try
		{
			List<string> toggles = new();
			if (revengeance())
				toggles.Add("Revengeance");
			if (death())
				toggles.Add("Death");
			if (legendary())
				toggles.Add("Legendary");
			if (malice())
				toggles.Add("Malice");
			object current = currentDifficulty();
			string mode = current == null ? (toggles.Count == 0 ? Text("NoCustomMode") : string.Join(" + ", toggles)) : DisplayValue(difficultyName(current));
			object scale = current == null ? null : difficultyScale(current);
			string active = toggles.Count == 0 ? Text("NoExtraToggles") : string.Join(", ", toggles);
			string detail = scale == null ? Text("CalamityDetailNoScale", active) : Text("CalamityDetail", active, Convert.ToDouble(scale, CultureInfo.InvariantCulture).ToString("0.##", CultureInfo.CurrentCulture));
			return new DamageDifficultyEntry(Source, mode, detail, AdapterHealthy: true);
		}
		catch (Exception exception)
		{
			return Failed(exception);
		}
	}
}
