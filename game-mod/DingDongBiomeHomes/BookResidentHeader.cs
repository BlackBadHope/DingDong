using System;
using Microsoft.Xna.Framework;
using Terraria.UI;
using Terraria.Localization;
namespace DingDongBiomeHomes;
internal sealed class BookResidentHeader:UIElement
{
    private readonly QuestTargetIcon portrait;
    private readonly BookText name,reputation,amount,quote;
    private readonly BookProgressBar bar;
    internal BookResidentHeader(string key,int percent,int type,string words)
    {
        Width.Set(0,1);Height.Set(164,0);
        portrait=new QuestTargetIcon(0,type,166,true);portrait.Height.Set(158,0);Append(portrait);
        name=new BookText(NpcQuestSystem.ResidentName(key),1.5f,ResidentBookJournal.Ink);Append(name);
        // Reference: "Репутация мира" caption, then a thick bar with "40 / 100" to its right.
        reputation=new BookText(BookUiText.Get("Reputation"),.94f,ResidentBookJournal.Ink);Append(reputation);
        bar=new BookProgressBar(percent/100f);bar.Height.Set(16,0);Append(bar);
        amount=new BookText($"{percent} / 100",1.0f,ResidentBookJournal.Ink);Append(amount);
        quote=new BookText(words,.87f,ResidentBookJournal.Ink);Append(quote);
    }
    public override void Recalculate()
    {
        base.Recalculate();float portraitWidth=Math.Clamp(GetInnerDimensions().Width*.27f,84,134),left=portraitWidth+14;
        portrait.Width.Set(portraitWidth,0);
        name.Left.Set(left,0);name.Width.Set(-left,1);reputation.Left.Set(left,0);reputation.Width.Set(-left,1);quote.Left.Set(left,0);quote.Width.Set(-left,1);
        name.Top.Set(0,0);name.Recalculate();float y=name.GetOuterDimensions().Height+6;
        reputation.Top.Set(y,0);reputation.Recalculate();y+=reputation.GetOuterDimensions().Height+4;
        const float amountWidth=84;
        bar.Left.Set(left,0);bar.Width.Set(-left-amountWidth-8,1);bar.Top.Set(y+2,0);
        amount.Left.Set(-amountWidth,1);amount.Width.Set(amountWidth,0);amount.Top.Set(y-4,0);amount.Recalculate();
        y+=28;quote.Top.Set(y,0);quote.Recalculate();
        Height.Set(Math.Max(164,y+quote.GetOuterDimensions().Height+6),0);base.Recalculate();
    }
}
