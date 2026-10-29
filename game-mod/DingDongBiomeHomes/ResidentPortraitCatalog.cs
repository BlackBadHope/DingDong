using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ID;
namespace DingDongBiomeHomes;

// Journal illustrations are independent of world textures and NPC animation data.
// Rectangles are measured on the generated six-cell atlas; no native frame is altered.
internal static class ResidentPortraitCatalog
{
    internal static bool TryFrame(int type,out Texture2D texture,out Rectangle frame)
    {
        texture=null;
        frame=type switch {
            NPCID.Dryad=>new Rectangle(2,4,36,48),
            NPCID.GoblinTinkerer=>new Rectangle(42,4,36,48),
            NPCID.Merchant=>new Rectangle(82,4,36,48),
            NPCID.Nurse=>new Rectangle(2,60,36,48),
            NPCID.BestiaryGirl=>new Rectangle(42,60,36,48),
            NPCID.Clothier=>new Rectangle(82,60,36,48),
            _=>Rectangle.Empty
        };
        if(frame==Rectangle.Empty||DingDongUiAssets.ResidentPortraits==null)return false;
        texture=DingDongUiAssets.ResidentPortraits.Value;return true;
    }
}
