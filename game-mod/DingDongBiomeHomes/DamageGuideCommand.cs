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
public sealed class DamageGuideCommand : ModCommand
{
	public override string Command => "dingdongdamage";
	public override CommandType Type => CommandType.Chat;
	public override string Usage => "/dingdongdamage";
	public override string Description => Language.GetTextValue("Mods.DingDongBiomeHomes.Damage.Guide.CommandDescription");

	public override void Action(CommandCaller caller, string input, string[] args) => DingDongHubUiSystem.OpenCombat(DamageGuidePageId.Overview);
}
