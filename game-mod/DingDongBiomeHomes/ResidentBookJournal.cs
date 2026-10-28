using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;
using Terraria.GameContent;
using ReLogic.Graphics;

namespace DingDongBiomeHomes;

// Two independent scrolling leaves keep long translations out of the binding.
internal sealed class ResidentBookJournal : UIElement
{
    internal static readonly Color Ink = new(57, 36, 19);
    internal static readonly Color Muted = new(105, 78, 43);
    private readonly UIList residents = new() { ListPadding = 7f, ManualSortMethod = _ => { } };
    private readonly UIList detail = new() { ListPadding = 8f, ManualSortMethod = _ => { } };
    private readonly BookScrollbar residentScroll = new();
    private readonly BookScrollbar detailScroll = new();
    private readonly UIElement questFooter=new();
    private int selectedIndex;
    private bool loaded;
    private string selected = "Terraria/Guide";
    private Dictionary<string, ResidentQuestProgress> records = new(StringComparer.Ordinal);
    internal string SelectedResident => selected;
    private static string L(string ru, string en) => Language.ActiveCulture.CultureInfo.TwoLetterISOLanguageName == "ru" ? ru : en;

    internal ResidentBookJournal()
    {
        Width.Set(0, 1); Height.Set(0, 1);
        residents.Width.Set(-26, .455f); residents.Height.Set(0, 1); Append(residents);
        residentScroll.Left.Set(-24, .455f); residentScroll.Width.Set(20, 0); residentScroll.Height.Set(0, 1);
        residentScroll.SetView(100, 1000); Append(residentScroll); residents.SetScrollbar(residentScroll);
        detail.Left.Set(0, .525f);detail.Top.Set(-60,0); detail.Width.Set(-27, .455f); detail.Height.Set(-124, 1); Append(detail);
        detailScroll.Left.Set(-22, .98f);detailScroll.Top.Set(-60,0); detailScroll.Width.Set(20, 0); detailScroll.Height.Set(-124, 1);
        detailScroll.SetView(100, 1000); Append(detailScroll); detail.SetScrollbar(detailScroll);
        questFooter.Left.Set(0,.525f);questFooter.Top.Set(-174,1);questFooter.Width.Set(-27,.455f);questFooter.Height.Set(174,0);Append(questFooter);
    }

    internal void Reload()
    {
        records = Main.LocalPlayer.GetModPlayer<NpcQuestPlayer>().CurrentSnapshot().ToDictionary(x => x.NpcKey,x=>x.Progress,StringComparer.Ordinal);
        float position=residents.ViewPosition;
        residents.Clear();int index=0;
        foreach(var npc in NpcTownManifest.All.OrderBy(n=>NpcQuestSystem.ResidentName(n.NpcKey),StringComparer.CurrentCulture))
        {
            var p=Get(npc.NpcKey);string key=npc.NpcKey;if(key==selected)selectedIndex=index;index++;
            var row=new BookResidentRow(NpcQuestSystem.ResidentName(key),ResidentTrustPolicy.Percent(p,npc),NpcResidentRegistry.TryGetType(key,out int t)?t:0,key==selected,p.Active,ResidentEncounterSystem.IsPending(key),ResidentStoryRoles.IsMain(key));
            row.OnLeftClick+=(_,_)=>SelectResident(key);
            residents.Add(row);
        }
        RebuildDetail(); Recalculate(); residents.ViewPosition=loaded?position:selectedIndex*(BookResidentRow.RowHeight+residents.ListPadding);loaded=true;
    }
    internal void SelectResident(string key)
    {
        if(!NpcTownManifest.TryGet(key,out _))return;
        selected=key;SoundEngine.PlaySound(SoundID.MenuTick);Reload();residents.ViewPosition=selectedIndex*(BookResidentRow.RowHeight+residents.ListPadding);detail.ViewPosition=0;
    }
    private ResidentQuestProgress Get(string key) => records.TryGetValue(key,out var p)?p:new();
    private void RebuildDetail()
    {
        float position=detail.ViewPosition;detail.Clear();
        if(!NpcTownManifest.TryGet(selected,out var npc))return;
        var p=Get(selected);int percent=ResidentTrustPolicy.Percent(p,npc);
        var heading=new BookResidentHeader(selected,percent,NpcResidentRegistry.TryGetType(selected,out int type)?type:0,ResidentQuote(selected));
        detail.Add(heading);
        // Owner item 11: main storyline residents are drawn per world (ResidentStoryRoles) and marked.
        AddText(ResidentStoryRoles.IsMain(selected)?L("★ Сюжетная линия","★ Main storyline"):L("Побочная линия","Side story"),.84f,!ResidentStoryRoles.IsMain(selected));
        bool pending=ResidentEncounterSystem.IsPending(selected);
        if(pending)AddText(EncounterHint(selected),.80f,true);
        bool complete=PersonalResidentQuestCatalog.Complete(selected,p.Completed);
        var d=p.Active?p.Definition(npc):PersonalResidentQuestCatalog.Resolve(npc,p.Completed);
        var step=PersonalResidentQuestCatalog.Step(d.StoryKey);
        if(complete)AddText(L("История завершена","Story complete"),1.02f);
        if(!complete)
        {
            var objective=new BookSection();
            var titleRow=new UIElement();titleRow.Width.Set(0,1);titleRow.Height.Set(34,0);
            var targetIcon=new QuestTargetIcon(step.ItemType>0?step.ItemType:step.TargetNpcType==0?ItemID.Compass:0,step.TargetNpcType,30);targetIcon.Width.Set(32,0);targetIcon.Height.Set(32,0);titleRow.Append(targetIcon);
            var title=new BookText(step.Title,1.15f,Ink);title.Left.Set(42,0);title.Width.Set(-42,1);titleRow.Append(title);objective.Add(titleRow);
            objective.Add(new BookText(NpcQuestSystem.ObjectiveText(Main.LocalPlayer,d,p),.98f,Ink));
            int objectiveProgress=d.Kind is ResidentQuestKind.DeliverSupplies or ResidentQuestKind.Expedition?NpcQuestSystem.QuestInventoryCount(Main.LocalPlayer,d,p):p.Progress;
            objective.Add(new BookProgressBar(p.Active?Math.Min(1,objectiveProgress/(float)Math.Max(1,d.Goal)):0));
            objective.Add(new BookText(p.Active?L("Выполняет: ","Assigned to: ")+p.AssignedName:L("Можно принять при встрече.","Available when you meet this resident."),.94f,Ink));
            objective.Add(new BookText(L("Прогресс общий. Награда — исполнителю.","Shared progress. Reward goes to the performer."),.79f,Muted));detail.Add(objective);
            var rewards=new BookSection(88);rewards.Add(new BookText(L("Награды","Rewards"),.89f,Ink));
            var rewardRow=new UIElement();rewardRow.Width.Set(0,1);rewardRow.Height.Set(45,0);
            var reward=new QuestTargetIcon(d.RewardItemType,0,36);reward.Width.Set(38,0);reward.Height.Set(38,0);rewardRow.Append(reward);
            var itemLabel=new BookText(d.RewardItemCount+" × "+Lang.GetItemNameValue(d.RewardItemType),.83f,Ink);itemLabel.Left.Set(44,0);itemLabel.Width.Set(-48,.60f);rewardRow.Append(itemLabel);
            int copper=p.Active?p.RewardCopper:d.BaseRewardCopper;
            var coin=new QuestTargetIcon(copper>=10000?ItemID.GoldCoin:ItemID.SilverCoin,0,30);coin.Left.Set(0,.61f);coin.Width.Set(32,0);coin.Height.Set(38,0);rewardRow.Append(coin);
            var coinLabel=new BookText(QuestRewardBalance.FormatCoins(copper),.84f,Ink);coinLabel.Left.Set(34,.61f);coinLabel.Width.Set(-34,.39f);rewardRow.Append(coinLabel);rewards.Add(rewardRow);detail.Add(rewards);
        }
        var history=NpcQuestWorldLedger.History.Where(h=>h.NpcKey==selected).TakeLast(5).Reverse().ToArray();
        if(history.Length>0){AddText(L("Общий журнал выполнения","Shared completion log"),.9f);foreach(var h in history)AddText($"{Math.Min(h.Ordinal,5)}/5 · {h.Performer}",.76f,true);}
        BuildFooter(npc,p,pending,complete,percent);

        Recalculate();detail.ViewPosition=position;
    }
    private static string ResidentQuote(string key)=>key switch {
        "Terraria/Dryad"=>L("«Лес живёт, пока о нём заботятся. Помоги мне сохранить его.»","The forest lives while someone cares for it. Help me preserve it."),
        "Terraria/GoblinTinkerer"=>L("«Каждую неисправность можно превратить в изобретение.»","Every malfunction can become an invention."),
        "Terraria/Merchant"=>L("«Хорошее дело начинается с честной сделки.»","A good venture begins with an honest deal."),
        "Terraria/Nurse"=>L("«Возвращайся целым. Об остальном позабочусь я.»","Come back in one piece. I will take care of the rest."),
        "Terraria/BestiaryGirl"=>L("«У каждого зверя свой нрав. Присмотрись внимательнее!»","Every creature has a personality. Take a closer look!"),
        "Terraria/Clothier"=>L("«Некоторые истории лучше зашить крепкой нитью.»","Some stories are best mended with a strong thread."),_=>""};
    private static string EncounterHint(string key) => key switch {
        "Terraria/Dryad"=>L("Дриада странствует по джунглям: днём наверху, ночью в пещерах. После первой победы над боссом она вернётся в резиденцию.","The Dryad wanders the Jungle: surface by day, caves by night. She returns to her residence after the first boss defeat."),
        "Terraria/Clothier"=>L("Старик ждёт у входа в данж. Освободите его от проклятия Скелетрона.","The Old Man waits at the Dungeon entrance. Break Skeletron’s curse."),
        "Terraria/SantaClaus"=>L("Найдите Санту в снежном биоме и поговорите с ним.","Find Santa in the Snow biome and speak to him."),
        "Terraria/Steampunker"=>L("Стимпанкер прячется в данже. Поговорите с ней при встрече.","The Steampunker is hiding in the Dungeon. Speak to her when you find her."),
        "Terraria/TaxCollector"=>L("В аду бродит измученная душа. Исцелите её очищающим порошком.","A Tortured Soul wanders the Underworld. Cure it with Purification Powder."),
        _=>L("Найдите и освободите этого жителя. Его появление не требует победы над боссом.","Find and rescue this resident. No boss defeat is required for the encounter.") };
    private static int NearbyResident(string key)
    {
        foreach(var npc in Main.ActiveNPCs)
            if(NpcResidentRegistry.TryGet(npc,out string k,out _)&&k==key&&Vector2.DistanceSquared(npc.Center,Main.LocalPlayer.Center)<=224*224)return npc.whoAmI;
        return -1;
    }
    private void AddText(string value,float scale,bool muted=false)
    {
        var text=new BookText(value,scale,muted?Muted:Ink);detail.Add(text);
    }
    private void BuildFooter(NpcTownIdentity npc,ResidentQuestProgress p,bool pending,bool complete,int percent)
    {
        questFooter.RemoveAllChildren();
        bool moved=NpcResidenceWorldSystem.IsOriginReleased(selected);
        bool mine=!p.Active||p.IsAssignedTo(Main.LocalPlayer.GetModPlayer<NpcQuestPlayer>().PersistentId);
        var d=p.Active?p.Definition(npc):PersonalResidentQuestCatalog.Resolve(npc,p.Completed);
        bool ready=p.Active&&NpcQuestSystem.IsReady(Main.LocalPlayer,d,p);
        int near=NearbyResident(selected);bool invite=complete&&percent==100&&!moved&&!pending;
        string label=invite?L("Пригласить к себе","Invite to your settlement"):complete?L("История завершена","Story complete"):ready?L("Сдать задание","Turn in quest"):p.Active?L("Задание выполняется","Quest in progress"):L("Принять задание","Accept quest");
        bool enabled=near>=0&&!pending&&(invite||!complete&&mine&&(!p.Active||ready));
        var step=PersonalResidentQuestCatalog.Step(d.StoryKey);
        if(p.Active&&mine&&!ready&&d.Kind==ResidentQuestKind.DialogueChoice&&p.Progress==0&&step!=null&&step.Choices.Length>=2)
        {
            // Dialogue-Choice: the authored options replace the single action button.
            int count=step.Choices.Length;float slot=1f/count;bool near0=near>=0&&!pending;
            for(int i=0;i<count;i++)
            {
                int index=i;var option=new BookActionButton(step.Choices[i].Text,.74f){UseSteel=true,Available=near0,TextColor=near0?Color.White:new Color(206,202,190)};
                option.Left.Set(24+2*i,slot*i);option.Width.Set(-48*slot-2,slot);option.Height.Set(44,0);
                option.OnLeftClick+=(_,_)=>{if(near0){NpcQuestNetwork.RequestQuestChoice(near,index);Reload();}};questFooter.Append(option);
            }
        }
        else
        {
        var action=new BookActionButton(label,.94f){UseSteel=true,Available=enabled,TextColor=enabled?Color.White:new Color(206,202,190)};
        action.Left.Set(24,0);action.Width.Set(-48,1);action.Height.Set(44,0);
        action.OnLeftClick+=(_,_)=>{if(enabled){NpcQuestNetwork.RequestQuestInteraction(near,invite);Reload();}};questFooter.Append(action);
        }
        string hint=pending?L("Сначала найдите и освободите жителя.","Find and rescue this resident first."):!mine?L("Задание выполняет: ","Assigned to: ")+p.AssignedName:near<0?L("Подойдите к жителю, чтобы поговорить.","Meet this resident to speak."):"";
        var notice=new BookText(hint,.67f,Muted);notice.Top.Set(47,0);notice.Left.Set(4,0);notice.Width.Set(-8,1);questFooter.Append(notice);
        var trail=new ResidentStoryTrail(selected,p.Completed);trail.Top.Set(74,0);trail.Width.Set(-170,1);trail.Height.Set(100,0);questFooter.Append(trail);
        var house=new BookAssetIcon(moved?BookGlyph.Residents:BookGlyph.Residence);house.Left.Set(-148,1);house.Top.Set(68,0);house.Width.Set(118,0);house.Height.Set(63,0);questFooter.Append(house);
        var homeText=new BookText(moved?L("Переезд завершён","Relocation complete"):L("Защищена до переезда","Protected until relocation"),.66f,Ink);homeText.Left.Set(-148,1);homeText.Top.Set(132,0);homeText.Width.Set(118,0);questFooter.Append(homeText);
    }

}

internal sealed class BookText : UIElement
{
    private string text;private readonly float scale;private string[] lines=Array.Empty<string>();
    internal Color TextColor {get;set;}
    internal string Value=>text;
    internal BookText(string text,float scale,Color color)
    {this.text=text;this.scale=scale;TextColor=color;Width.Set(0,1);Height.Set(24,0);IgnoresMouseInteraction=true;}
    internal void SetText(string value){text=value;Recalculate();}
    public override void Recalculate()
    {
        base.Recalculate();float width=Math.Max(20,GetInnerDimensions().Width);var output=new List<string>();
        foreach(var paragraph in (text??"").Split('\n'))
        {
            string line="";
            foreach(string originalWord in paragraph.Split(' '))
            {
                string word=originalWord;
                if(line.Length>0&&FontAssets.MouseText.Value.MeasureString(line+" "+word).X*scale>width){output.Add(line);line="";}
                while(word.Length>1&&FontAssets.MouseText.Value.MeasureString(word).X*scale>width){
                    int split=1;while(split<word.Length&&FontAssets.MouseText.Value.MeasureString(word[..(split+1)]).X*scale<=width)split++;
                    output.Add(word[..split]);word=word[split..];
                }
                line=line.Length==0?word:line+" "+word;
            }
            output.Add(line);
        }
        lines=output.ToArray();Height.Set(Math.Max(1,lines.Length)*FontAssets.MouseText.Value.LineSpacing*scale+2,0);base.Recalculate();
    }
    protected override void DrawSelf(SpriteBatch b)
    {var p=GetInnerDimensions().Position();foreach(string line in lines){b.DrawString(FontAssets.MouseText.Value,line,p,TextColor,0,Vector2.Zero,scale,SpriteEffects.None,0);p.Y+=FontAssets.MouseText.Value.LineSpacing*scale;}}
}
internal sealed class BookProgressBar : UIElement
{
    private readonly float value;
    internal BookProgressBar(float value){this.value=Math.Clamp(value,0,1);Width.Set(0,1);Height.Set(12,0);IgnoresMouseInteraction=true;}
    protected override void DrawSelf(SpriteBatch b)
    {var a=GetDimensions().ToRectangle();DingDongUiTheme.DrawPanel(b,a,new Color(112,100,76),new Color(71,52,29));a.Inflate(-2,-2);a.Width=(int)(a.Width*value);DingDongUiTheme.Fill(b,a,new Color(89,147,49));}
}
internal sealed class BookResidentRow : UIElement
{
    internal const float RowHeight=98;
    private readonly bool selected;
    internal BookResidentRow(string name,int reputation,int npcType,bool selected,bool active,bool pending,bool mainStory=false)
    {
        this.selected=selected;Width.Set(0,1);Height.Set(RowHeight,0);SetPadding(7);
        var icon=new QuestTargetIcon(0,npcType,82,true);icon.Width.Set(66,0);icon.Height.Set(76,0);Append(icon);
        var title=new BookText(name,1.04f,ResidentBookJournal.Ink);title.Left.Set(76,0);title.Width.Set(mainStory?-108:-76,1);Append(title);
        bool ru=Language.ActiveCulture.CultureInfo.TwoLetterISOLanguageName=="ru";
        if(mainStory)
        {
            // Main storyline marker (owner item 11): a fallen star in the row's corner.
            var star=new QuestTargetIcon(ItemID.FallenStar,0,26);star.Left.Set(-30,1);star.Top.Set(2,0);star.Width.Set(28,0);star.Height.Set(28,0);Append(star);
        }
        var bar=new BookProgressBar(reputation/100f);bar.Left.Set(76,0);bar.Top.Set(40,0);bar.Width.Set(-78,.62f);bar.Height.Set(12,0);Append(bar);
        var amount=new BookText($"{reputation} / 100",.83f,ResidentBookJournal.Ink);amount.Left.Set(8,.62f);amount.Top.Set(37,0);amount.Width.Set(-8,.38f);Append(amount);
        string state=active?(ru?"Задание":"Quest"):pending?(ru?"Нужно найти":"Find resident"):(ru?"Доступно":"Available");
        var label=new BookText(mainStory?(ru?"Сюжетная · ":"Main · ")+state:state,.67f,pending?ResidentBookJournal.Muted:DingDongUiTheme.Positive);label.Left.Set(76,0);label.Width.Set(-76,1);label.Top.Set(60,0);Append(label);
    }
    protected override void DrawSelf(SpriteBatch b)
    {
        var area=GetDimensions().ToRectangle();
        DingDongUiAssets.DrawBookCard(b,area,selected?new Color(255,236,177):IsMouseHovering?Color.White:Color.White*.72f);
    }
}

internal sealed class BookSection:JournalFrame
{
    private readonly List<UIElement> rows=new();
    private readonly float minimumHeight;
    internal BookSection(float minimumHeight=0){this.minimumHeight=minimumHeight;Width.Set(0,1);Height.Set(Math.Max(80,minimumHeight),0);SetPadding(12);}
    internal void Add(UIElement row){rows.Add(row);Append(row);}
    public override void Recalculate()
    {
        base.Recalculate();float y=0;
        foreach(var row in rows){row.Top.Set(y,0);row.Recalculate();y+=row.GetOuterDimensions().Height+8;}
        Height.Set(Math.Max(minimumHeight,Math.Max(0,y-8)+PaddingTop+PaddingBottom),0);base.Recalculate();
    }
}

internal sealed class BookActionButton: UITextPanel<string>
{
    private readonly float scale;
    internal bool UseSteel {get;set;}
    internal bool Available {get;set;}=true;
    internal BookActionButton(string text,float scale):base(text,scale){this.scale=scale;}
    protected override void DrawSelf(SpriteBatch batch)
    {
        var area=GetDimensions().ToRectangle();
        if(UseSteel)BookSkin.ActionFrame(batch,area,IsMouseHovering&&Available?new Color(255,236,190):Color.White*(Available?1f:.76f));
        else DingDongUiAssets.DrawBookCard(batch,area,IsMouseHovering?new Color(255,239,193):BackgroundColor);
        var font=FontAssets.MouseText.Value;var size=font.MeasureString(Text)*scale;
        batch.DrawString(font,Text,area.Center.ToVector2()-size*.5f,TextColor,0,Vector2.Zero,scale,SpriteEffects.None,0);
    }
}
