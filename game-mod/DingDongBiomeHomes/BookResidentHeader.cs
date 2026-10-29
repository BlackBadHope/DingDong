using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;
using Terraria.UI;
namespace DingDongBiomeHomes;
// Reference header: large framed portrait | name, epithet, "Репутация мира", thick bar with
// "40 / 100" to its right, then the resident's quote in «». Texts come from the lore contract
// (epithet, quote) and are prepared by the journal; nothing is formatted while drawing.
internal sealed class BookResidentHeader:UIElement
{
    private readonly QuestTargetIcon portrait;
    private readonly BookText name,epithet,reputation,amount,quote;
    private readonly BookProgressBar bar;
    private readonly bool mainStory;
    private readonly float amountWidth;
    internal BookResidentHeader(string key,int percent,int type,string epithetText,string words,bool mainStory=false)
    {
        Width.Set(0,1);Height.Set(164,0);this.mainStory=mainStory;
        portrait=new QuestTargetIcon(0,type,166,true);portrait.Height.Set(150,0);portrait.Top.Set(6,0);Append(portrait);
        name=new BookText(NpcQuestSystem.ResidentName(key),1.5f,ResidentBookJournal.Ink){SingleLine=true,MinScale=1f};Append(name);
        if(!string.IsNullOrEmpty(epithetText)){epithet=new BookText(epithetText,.82f,DingDongUiTheme.Gold){SingleLine=true};Append(epithet);}
        reputation=new BookText(BookUiText.Get("Reputation"),.94f,ResidentBookJournal.Ink);Append(reputation);
        bar=new BookProgressBar(percent/100f);bar.Height.Set(16,0);Append(bar);
        amount=new BookText(BookUiText.Format("Count",percent,100),1.0f,ResidentBookJournal.Ink){SingleLine=true,MinScale=.7f};Append(amount);
        // Measured once: "100 / 100" must fit whatever the font and language (item 8 review).
        amountWidth=FontAssets.MouseText.Value.MeasureString(BookUiText.Format("Count",100,100)).X+8f;
        quote=new BookText(words??string.Empty,.87f,ResidentBookJournal.Ink);Append(quote);
    }
    public override void Recalculate()
    {
        base.Recalculate();float portraitWidth=Math.Clamp(GetInnerDimensions().Width*.27f,84,134),left=portraitWidth+16;
        portrait.Width.Set(portraitWidth,0);
        name.Left.Set(left,0);name.Width.Set(-left,1);reputation.Left.Set(left,0);reputation.Width.Set(-left,1);quote.Left.Set(left,0);quote.Width.Set(-left,1);
        name.Top.Set(0,0);name.Recalculate();float y=name.GetOuterDimensions().Height+2;
        if(epithet!=null){epithet.Left.Set(left,0);epithet.Width.Set(-left,1);epithet.Top.Set(y,0);epithet.Recalculate();y+=epithet.GetOuterDimensions().Height+4;}
        else y+=4;
        reputation.Top.Set(y,0);reputation.Recalculate();y+=reputation.GetOuterDimensions().Height+4;
        bar.Left.Set(left,0);bar.Width.Set(-left-amountWidth-8,1);bar.Top.Set(y+2,0);
        amount.Left.Set(-amountWidth,1);amount.Width.Set(amountWidth,0);amount.Top.Set(y-4,0);amount.Recalculate();
        y+=28;quote.Top.Set(y,0);quote.Recalculate();
        Height.Set(Math.Max(164,y+quote.GetOuterDimensions().Height+6),0);base.Recalculate();
    }
    protected override void DrawSelf(SpriteBatch batch)
    {
        // Portrait frame: a parchment card behind the sprite, gold-edged for the world's main storyline.
        var p=portrait.GetDimensions().ToRectangle();p.Inflate(4,2);
        DingDongUiAssets.DrawBookCard(batch,p,new Color(255,246,222)*.95f);
        if(mainStory)BookFrames.Gold(batch,p);
    }
}
