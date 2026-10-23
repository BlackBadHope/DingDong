using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
internal sealed class FailedDifficultyAdapter : IOptionalDifficultyAdapter
{
	private readonly string source;
	private readonly Exception exception;
	internal FailedDifficultyAdapter(string source, Exception exception)
	{
		this.source = source;
		this.exception = exception;
	}

	public DamageDifficultyEntry Read(bool worldReady) => new(source, Language.GetTextValue("Mods.DingDongBiomeHomes.Damage.Guide.Difficulty.CompatibilityUnavailable"), $"{exception.GetType().Name}: {exception.Message}", AdapterHealthy: false);
}
