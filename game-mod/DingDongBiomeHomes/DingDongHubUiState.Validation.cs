using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace DingDongBiomeHomes;
internal sealed partial class DingDongHubUiState
{
	internal DingDongHubProbeState CaptureProbeState()
	{
		int pageCount = DamageGuideContent.PageOrder.Length;
		int pageNumber = Array.IndexOf(DamageGuideContent.PageOrder, selectedDamagePage) + 1;
		string title = selectedTab == DingDongHubTab.Combat ? DamageGuideContent.Build(selectedDamagePage, Main.LocalPlayer).Title : string.Empty;
		return new DingDongHubProbeState(Main.InGameUI.CurrentState == this || Main.MenuUI.CurrentState == this, selectedTab, selectedDamagePage, pageNumber, pageCount, title);
	}
}
