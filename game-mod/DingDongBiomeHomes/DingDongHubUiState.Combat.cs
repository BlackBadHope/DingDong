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
	internal void ChangeDamagePage(int delta)
	{
		SoundEngine.PlaySound(SoundID.MenuTick);
		int current = Array.IndexOf(DamageGuideContent.PageOrder, selectedDamagePage);
		int next = DingDongPagination.Move(current, delta, DamageGuideContent.PageOrder.Length);
		selectedDamagePage = DamageGuideContent.PageOrder[next];
		Refresh(resetScroll: true);
	}
}
