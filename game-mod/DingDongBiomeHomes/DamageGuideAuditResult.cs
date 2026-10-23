using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace DingDongBiomeHomes;
public readonly record struct DamageGuideAuditResult(int Pages, int EmptyPages, int DifficultyAdapterFailures)
{
	public int Failures => EmptyPages + DifficultyAdapterFailures;

	public string ToMarker() => string.Join('|', new[] { "DINGDONG_DAMAGE_GUIDE_AUDIT", $"pages={Pages}", $"emptyPages={EmptyPages}", $"difficultyAdapters={DamageDifficultyContextSystem.AdapterCount}", $"difficultyAdapterFailures={DifficultyAdapterFailures}", $"verdict={(Failures == 0 ? "PASS" : "FAIL")}" });
}
