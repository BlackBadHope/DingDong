using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.UI;

namespace DingDongBiomeHomes;

/// <summary>
/// Owner item 11: residences of this world's main storyline are marked on every map mode
/// with a small star, only once this character has discovered them (no spoilers). The hover
/// text is formatted only while the pin is actually hovered.
/// </summary>
public sealed class StoryResidenceMapLayer : ModMapLayer
{
	private static readonly Color StarTint = new(255, 214, 96);

	public override Position GetDefaultPosition() => new Before(IMapLayer.Pings);

	public override void Draw(ref MapOverlayDrawContext context, ref string text)
	{
		Player player = Main.LocalPlayer;
		if (Main.dedServ || Main.gameMenu || player is null || !player.active || !SettlementWorldState.HasCompleteResidenceRegistry)
			return;
		Main.instance.LoadItem(ItemID.FallenStar);
		Texture2D star = TextureAssets.Item[ItemID.FallenStar].Value;
		int frames = Main.itemAnimations[ItemID.FallenStar]?.FrameCount ?? 1;
		var frame = new SpriteFrame(1, (byte)System.Math.Clamp(frames, 1, 255), 0, 0);
		ResidenceDiscoveryPlayer discovery = player.GetModPlayer<ResidenceDiscoveryPlayer>();
		foreach (SettlementResidenceReport residence in SettlementWorldState.Residences)
		{
			if (!ResidentStoryRoles.IsMain(residence.NpcKey) || !discovery.HasDiscovered(residence.ReviewId))
				continue;
			Vector2 at = new(residence.Area.Center.X, residence.Area.Y - 2);
			if (context.Draw(star, at, StarTint, frame, .9f, 1.15f, Alignment.Bottom).IsMouseOver)
				text = BookUiText.Format("Map.MainResidence", NpcQuestSystem.ResidentName(residence.NpcKey));
		}
	}
}
