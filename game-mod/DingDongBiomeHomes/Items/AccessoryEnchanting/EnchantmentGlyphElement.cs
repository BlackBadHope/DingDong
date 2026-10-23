using System;
using System.Collections.Generic;
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

namespace DingDongBiomeHomes.Items.AccessoryEnchanting;
internal sealed class EnchantmentGlyphElement : UIElement
{
	private AccessoryEnchantmentId current;
	private Asset<Texture2D> texture;
	internal void SetEnchantment(AccessoryEnchantmentId enchantment)
	{
		if (current == enchantment)
			return;
		current = enchantment;
		texture = AccessoryEnchantmentCatalog.IsValid(enchantment) ? ModContent.Request<Texture2D>($"DingDongBiomeHomes/Assets/AccessoryEnchanting/Enchant_{enchantment}", AssetRequestMode.ImmediateLoad) : null;
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		base.DrawSelf(spriteBatch);
		if (texture == null)
			return;
		CalculatedStyle dimensions = GetDimensions();
		spriteBatch.Draw(texture.Value, dimensions.ToRectangle(), Color.White);
	}
}
