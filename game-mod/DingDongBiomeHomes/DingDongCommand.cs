using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
public sealed partial class DingDongCommand : ModCommand
{
	private const string PreviewWorldPrefix = "DingDongPreviewProbe-";
	private const int DefaultReviewPlayerWidth = 20;
	private const int DefaultReviewPlayerHeight = 42;
	public override string Command => "dingdong";
	public override CommandType Type => CommandType.World | CommandType.Console;
	public override string Usage => "/dingdong <audit|review [1-61|next|prev]>";
	public override string Description => "Shows the settlement audit or focuses a persisted authored residence for owner review.";
}
