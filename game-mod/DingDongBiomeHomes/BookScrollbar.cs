using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
namespace DingDongBiomeHomes;

// Keep the game's scroll position, wheel routing and mouse-down behavior.
// Only the journal handle and its continuous drag presentation are themed.
internal sealed class BookScrollbar:UIScrollbar
{
    private bool dragging;
    private float dragOffset;
    private Rectangle Handle()
    {
        var inner=GetInnerDimensions();float maximum=Math.Max(1,MaxViewSize);
        return new Rectangle((int)inner.X,(int)(inner.Y+inner.Height*ViewPosition/maximum)-3,(int)inner.Width,(int)(inner.Height*ViewSize/maximum)+7);
    }
    public override void LeftMouseDown(UIMouseEvent evt)
    {
        var handle=Handle();base.LeftMouseDown(evt);
        if(evt.Target==this&&CanScroll&&handle.Contains(evt.MousePosition.ToPoint())){dragging=true;dragOffset=evt.MousePosition.Y-handle.Y;}
    }
    public override void LeftMouseUp(UIMouseEvent evt){base.LeftMouseUp(evt);dragging=false;}
    protected override void DrawSelf(SpriteBatch batch)
    {
        if(!CanScroll){dragging=false;return;}
        var inner=GetInnerDimensions();
        if(!Main.mouseLeft)dragging=false;
        if(dragging&&inner.Height>0)ViewPosition=(UserInterface.ActiveInstance.MousePosition.Y-inner.Y-dragOffset)/inner.Height*MaxViewSize;
        var track=GetDimensions().ToRectangle();
        DingDongUiAssets.DrawBookCard(batch,track,new Color(125,94,64));
        var handle=Handle();handle.X+=2;handle.Width=Math.Max(5,handle.Width-4);
        DingDongUiAssets.DrawBookCard(batch,handle,dragging||IsMouseHovering?new Color(255,225,149):new Color(224,190,128));
    }
}
