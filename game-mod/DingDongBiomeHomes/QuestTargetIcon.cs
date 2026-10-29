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
internal sealed class QuestTargetIcon : UIElement
{
	private readonly float MaxIconSize;
    private readonly bool upscale;
	private readonly int itemType;
	private readonly int npcType;
	internal QuestTargetIcon(int itemType, int npcType, float maxSize = 44f, bool upscale = false)
	{
		this.MaxIconSize=maxSize;this.upscale=upscale;
        this.itemType = itemType;
		this.npcType = npcType;
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		base.DrawSelf(spriteBatch);
		Texture2D texture;
		Rectangle frame;
		string hoverText;
		if (itemType > ItemID.None && itemType < TextureAssets.Item.Length)
		{
			Main.instance.LoadItem(itemType);
			texture = TextureAssets.Item[itemType].Value;
			frame = Main.itemAnimations[itemType]?.GetFrame(texture) ?? new Rectangle(0, 0, texture.Width, texture.Height);
			hoverText = Lang.GetItemNameValue(itemType);
		}
		else if (npcType > NPCID.None && npcType < TextureAssets.Npc.Length)
		{
            if(!ResidentPortraitCatalog.TryFrame(npcType,out texture,out frame))
            {
			Main.instance.LoadNPC(npcType);
			texture = TextureAssets.Npc[npcType].Value;
			int frameCount = Math.Max(1, Main.npcFrameCount[npcType]);
			frame = new Rectangle(0, 0, texture.Width, Math.Max(1, texture.Height / frameCount));
            }
			hoverText = ContentSamples.NpcsByNetId.TryGetValue(npcType, out NPC sample) ? sample.FullName : Lang.GetNPCNameValue(npcType);
		}
		else
		{
			return;
		}

		CalculatedStyle dimensions = GetDimensions();
		Vector2 center = new(dimensions.X + dimensions.Width * 0.5f, dimensions.Y + dimensions.Height * 0.5f);
		Vector2 origin = new(frame.Width * 0.5f, frame.Height * 0.5f);
		// Respect the actual slot (rewards are smaller than quest portraits); never upscale pixel art.
		float availableWidth = Math.Max(1f,Math.Min(MaxIconSize,dimensions.Width-4f));
        float availableHeight = Math.Max(1f,Math.Min(MaxIconSize,dimensions.Height-4f));
        float scale = Math.Min(upscale?4f:1f,Math.Min(availableWidth/frame.Width,availableHeight/frame.Height));
		BookPixelSampler.Draw(spriteBatch, texture, center, frame, Color.White, origin, scale);
		if (IsMouseHovering)
			Main.hoverItemName = hoverText;
	}
}
