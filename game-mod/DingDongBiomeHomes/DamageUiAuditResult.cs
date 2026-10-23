using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace DingDongBiomeHomes;
public readonly record struct DamageUiAuditResult(int DamagingItems, int HostileNpcs, int BannerBackedNpcs, int MissingLabels, int OutOfRangeProfiles, int StatefulBurstProfiles)
{
	public int Failures => MissingLabels + OutOfRangeProfiles + StatefulBurstProfiles;

	public string ToMarker() => string.Join('|', new[] { "DINGDONG_DAMAGE_UI_AUDIT", $"items={DamagingItems}", $"hostileNpcs={HostileNpcs}", $"bannerBackedNpcs={BannerBackedNpcs}", $"missingLabels={MissingLabels}", $"outOfRangeProfiles={OutOfRangeProfiles}", $"statefulBurstProfiles={StatefulBurstProfiles}", $"verdict={(Failures == 0 ? "PASS" : "FAIL")}" });
}
