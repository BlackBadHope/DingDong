using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace DingDongBiomeHomes;
public sealed class QuestJournalCommand : ModCommand
{
	public override string Command => "dingdongquests";
	public override CommandType Type => CommandType.Chat;
	public override string Usage => "/dingdongquests";
	public override string Description => Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.Journal.CommandDescription");

	public override void Action(CommandCaller caller, string input, string[] args) => DingDongHubUiSystem.Open(DingDongHubTab.Quests);
}
