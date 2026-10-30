using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;
using DingDongBiomeHomes.Fishing.Crates;
using DingDongBiomeHomes.Items.Keyring;

namespace DingDongBiomeHomes.Ui;

// Illustrated items use bounded native draws rather than replacing a small native
// texture with a large bitmap (which would also enlarge the dropped world item).
// Item IDs, collision sizes, storage, recipes and tile frame sheets stay native.
public sealed class DesignItemArtSystem : ModSystem
{
    private static Mod owner;
    private static Asset<Texture2D> keyring, lockpick, totem;

    public override void Load() { if (!Main.dedServ) owner = Mod; }
    public override void Unload() { owner = null; keyring = lockpick = totem = null; }

    internal static bool TryGet(Item item, out Texture2D texture)
    {
        texture = null;
        if (owner == null) return false;
        Asset<Texture2D> art = item.ModItem switch
        {
            KeyringItem => keyring ??= owner.Assets.Request<Texture2D>("Assets/Ui/Design/KeyringArt", AssetRequestMode.AsyncLoad),
            CrateLockpick => lockpick ??= owner.Assets.Request<Texture2D>("Assets/Ui/Design/CrateLockpickArt", AssetRequestMode.AsyncLoad),
            SpiritWardTotemItem => totem ??= owner.Assets.Request<Texture2D>("Assets/Ui/Design/SpiritWardTotemArt", AssetRequestMode.AsyncLoad),
            _ => null
        };
        if (art is not { IsLoaded: true }) return false;
        texture = art.Value;
        return true;
    }

}

public sealed class DesignItemArt : GlobalItem
{
    public override bool AppliesToEntity(Item entity, bool lateInstantiation) =>
        entity.ModItem is KeyringItem or CrateLockpick or SpiritWardTotemItem;

    public override bool PreDrawInInventory(Item item, SpriteBatch batch, Vector2 position, Rectangle frame,
        Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        if (!DesignItemArtSystem.TryGet(item, out Texture2D art)) return true;
        float extent = Math.Min(40f, Math.Max(frame.Width, frame.Height) * scale);
        // Native ItemSlot already applied GetAlpha (including the lockpick tier tint).
        batch.Draw(art, position, null, drawColor, 0,
            new Vector2(art.Width, art.Height) * .5f, extent / Math.Max(art.Width, art.Height), SpriteEffects.None, 0);
        return false;
    }

    public override bool PreDrawInWorld(Item item, SpriteBatch batch, Color lightColor, Color alphaColor,
        ref float rotation, ref float scale, int whoAmI)
    {
        if (!DesignItemArtSystem.TryGet(item, out Texture2D art)) return true;
        float extent = Math.Max(item.width, item.height) * scale;
        Vector2 position = item.Center - Main.screenPosition;
        // Native alphaColor already includes tier colour, opacity and shimmer fade.
        batch.Draw(art, position, null, alphaColor, rotation,
            new Vector2(art.Width, art.Height) * .5f, extent / Math.Max(art.Width, art.Height), SpriteEffects.None, 0);
        return false;
    }
}
