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

internal enum BookQuestStatus { Available, InProgress, Ready, Busy, Paused, Find, Complete }
internal enum BookQuestFilter { All, Main, Side, Mine, Done }

// Two independent scrolling leaves keep long translations out of the binding.
// Every text is built here, on Reload; the leaves only draw cached strings.
internal sealed class ResidentBookJournal : UIElement
{
    internal static readonly Color Ink = new(57, 36, 19);
    internal static readonly Color Muted = new(105, 78, 43);
    private static readonly BookQuestFilter[] FilterOrder = { BookQuestFilter.All, BookQuestFilter.Main, BookQuestFilter.Side, BookQuestFilter.Mine, BookQuestFilter.Done };
    private static BookQuestFilter filter = BookQuestFilter.All; // remembered for the session
    private static bool historyOpen;
    private readonly BookFilterButton[] filterButtons = new BookFilterButton[FilterOrder.Length];
    private readonly BookList residents = new(7f);
    private readonly BookList detail = new(8f);
    private readonly BookScrollbar residentScroll = new();
    private readonly BookScrollbar detailScroll = new();
    private readonly UIElement questFooter = new();
    private const float FilterHeight = 34f;
    private int selectedIndex;
    private int buybackCursor;
    private bool loaded, confirmAbandon;
    private string notice = string.Empty;
    private string selected = "Terraria/Guide";
    private Dictionary<string, ResidentQuestProgress> records = new(StringComparer.Ordinal);
    internal string SelectedResident => selected;

    internal ResidentBookJournal()
    {
        Width.Set(0, 1); Height.Set(0, 1);
        for (int i = 0; i < FilterOrder.Length; i++)
        {
            BookQuestFilter value = FilterOrder[i];
            var button = new BookFilterButton(BookUiText.Get("Filters." + value));
            // Inset from the page corner vine; gaps and the right edge stay as before.
            button.Left.Set(14 - 12f * i / FilterOrder.Length, i / (float)FilterOrder.Length * .455f); button.Width.Set(-4 - 38f / FilterOrder.Length, .455f / FilterOrder.Length);
            button.OnLeftClick += (_, _) => SetFilter(value);
            filterButtons[i] = button; Append(button);
        }
        residents.Top.Set(FilterHeight + 6, 0); residents.Width.Set(-26, .455f); residents.Height.Set(-FilterHeight - 6, 1); Append(residents);
        residentScroll.Top.Set(FilterHeight + 6, 0); residentScroll.Left.Set(-24, .455f); residentScroll.Width.Set(20, 0); residentScroll.Height.Set(-FilterHeight - 6, 1);
        residentScroll.SetView(100, 1000); Append(residentScroll); residents.SetScrollbar(residentScroll);
        detail.Left.Set(0, .525f); detail.Top.Set(-46, 0); detail.Width.Set(-27, .455f); detail.Height.Set(-138, 1); Append(detail);
        detailScroll.Left.Set(-22, .98f); detailScroll.Top.Set(-46, 0); detailScroll.Width.Set(20, 0); detailScroll.Height.Set(-138, 1);
        detailScroll.SetView(100, 1000); Append(detailScroll); detail.SetScrollbar(detailScroll);
        questFooter.Left.Set(0, .525f); questFooter.Top.Set(-174, 1); questFooter.Width.Set(-27, .455f); questFooter.Height.Set(174, 0); Append(questFooter);
    }

    private void SetFilter(BookQuestFilter value)
    {
        if (filter == value) return;
        filter = value; SoundEngine.PlaySound(SoundID.MenuTick);
        residents.ViewPosition = 0; loaded = false; Reload();
    }

    internal static bool Passes(BookQuestFilter value, string key, ResidentQuestProgress p, bool mine, bool complete) => value switch
    {
        BookQuestFilter.Main => ResidentStoryRoles.IsMain(key),
        BookQuestFilter.Side => !ResidentStoryRoles.IsMain(key),
        BookQuestFilter.Mine => p.Active && mine,
        BookQuestFilter.Done => complete,
        _ => true
    };

    /// <summary>Status shown in the list and used by the HUD markers (reads synchronized state only).</summary>
    internal static BookQuestStatus StatusOf(Player viewer, NpcTownIdentity npc, ResidentQuestProgress p)
    {
        string key = npc.NpcKey;
        if (ResidentEncounterSystem.IsPending(key)) return BookQuestStatus.Find;
        if (PersonalResidentQuestCatalog.Complete(key, p.Completed)) return BookQuestStatus.Complete;
        if (!p.Active) return BookQuestStatus.Available;
        if (p.IsAssignedTo(viewer.GetModPlayer<NpcQuestPlayer>().PersistentId))
            return NpcQuestSystem.IsReady(viewer, p.Definition(npc), p) ? BookQuestStatus.Ready : BookQuestStatus.InProgress;
        return BookQuestBridge.Paused(key) ? BookQuestStatus.Paused : BookQuestStatus.Busy;
    }

    internal static Color StatusColor(BookQuestStatus status) => status switch
    {
        BookQuestStatus.Ready => new Color(170, 104, 18),
        BookQuestStatus.InProgress => DingDongUiTheme.Information,
        BookQuestStatus.Busy or BookQuestStatus.Paused => DingDongUiTheme.Warning,
        BookQuestStatus.Find => new Color(112, 104, 92),
        BookQuestStatus.Complete => new Color(86, 96, 60),
        _ => DingDongUiTheme.Positive
    };

    internal void Reload()
    {
        Player viewer = Main.LocalPlayer;
        var questPlayer = viewer.GetModPlayer<NpcQuestPlayer>();
        string me = questPlayer.PersistentId;
        records = questPlayer.CurrentSnapshot().ToDictionary(x => x.NpcKey, x => x.Progress, StringComparer.Ordinal);
        for (int i = 0; i < filterButtons.Length; i++) filterButtons[i].Selected = FilterOrder[i] == filter;
        float position = residents.ViewPosition;
        residents.Clear(); int index = 0; selectedIndex = 0;
        foreach (var npc in NpcTownManifest.All.OrderBy(n => NpcQuestSystem.ResidentName(n.NpcKey), StringComparer.CurrentCulture))
        {
            string key = npc.NpcKey; var p = Get(key);
            bool complete = PersonalResidentQuestCatalog.Complete(key, p.Completed);
            if (!Passes(filter, key, p, p.IsAssignedTo(me) || p.IsPausedFor(me), complete)) continue;
            if (key == selected) selectedIndex = index;
            index++;
            BookQuestStatus status = StatusOf(viewer, npc, p);
            string name = NpcQuestSystem.ResidentName(key);
            string statusText = BookUiText.Get("Status." + status);
            string statusHover = status is BookQuestStatus.Busy or BookQuestStatus.Paused && p.AssignedName.Length > 0
                ? BookUiText.Format("StatusBusyBy", p.AssignedName) : statusText;
            string roleHover = BookUiText.Format("RoleHover", name, BookLore.ForceName(PersonalResidentQuestCatalog.Force(key)));
            var row = new BookResidentRow(name, ResidentReputationSystem.Local(key), NpcResidentRegistry.TryGetType(key, out int t) ? t : 0, key == selected,
                status, statusText, statusHover, ResidentStoryRoles.IsMain(key), key, roleHover);
            row.OnLeftClick += (_, _) => SelectResident(key);
            residents.Add(row);
        }
        if (index == 0) residents.Add(new BookText(BookUiText.Get("FilterEmpty"), .82f, Muted));
        RebuildDetail(); Recalculate();
        residents.ViewPosition = loaded ? position : selectedIndex * (BookResidentRow.RowHeight + residents.ListPadding); loaded = true;
    }

    internal void SelectResident(string key)
    {
        if (!NpcTownManifest.TryGet(key, out _)) return;
        if (key != selected) { confirmAbandon = false; notice = string.Empty; }
        selected = key; SoundEngine.PlaySound(SoundID.MenuTick); Reload();
        residents.ViewPosition = selectedIndex * (BookResidentRow.RowHeight + residents.ListPadding); detail.ViewPosition = 0;
    }

    private ResidentQuestProgress Get(string key) => records.TryGetValue(key, out var p) ? p : new();

    private void RebuildDetail()
    {
        float position = detail.ViewPosition; detail.Clear();
        if (!NpcTownManifest.TryGet(selected, out var npc)) return;
        Player viewer = Main.LocalPlayer;
        var p = Get(selected); int percent = ResidentTrustPolicy.Percent(p, npc);
        string name = NpcQuestSystem.ResidentName(selected);
        BookLoreResident lore = BookLore.Resident(selected);
        string epithet = lore != null ? BookLore.Text(lore.Epithet, name) : string.Empty;
        string quote = lore != null && !lore.Quote.IsEmpty ? BookLore.Text(lore.Quote, name) : BookUiText.Optional("Quotes." + ShortKey(selected));
        if (quote.Length > 0) quote = BookUiText.Format("QuoteFormat", quote);
        detail.Add(new BookResidentHeader(selected, percent, NpcResidentRegistry.TryGetType(selected, out int type) ? type : 0, epithet, quote, ResidentStoryRoles.IsMain(selected)));
        // Owner item 11: main storyline residents are drawn per world (ResidentStoryRoles) and marked.
        detail.Add(new BookStoryBadge(ResidentStoryRoles.IsMain(selected)));
        bool pending = ResidentEncounterSystem.IsPending(selected);
        if (pending)
        {
            string hint = lore != null && !lore.FindHint.IsEmpty ? BookLore.Text(lore.FindHint, name) : EncounterHint(selected);
            AddText(hint, .80f, true);
        }
        bool complete = PersonalResidentQuestCatalog.Complete(selected, p.Completed);
        var d = p.Active ? p.Definition(npc) : PersonalResidentQuestCatalog.Resolve(npc, p.Completed);
        int stageIndex = Math.Clamp(p.Active ? d.Variant : p.Completed, 0, Math.Max(0, PersonalResidentQuestCatalog.Length(selected) - 1));
        var step = PersonalResidentQuestCatalog.Step(selected + ":" + stageIndex) ?? PersonalResidentQuestCatalog.Step(d.StoryKey);
        BookLoreStage loreStage = lore?.Stage(stageIndex);
        if (complete)
        {
            // Epilogue after 5/5 (lore contract), otherwise a plain closing line.
            var epilogue = new BookSection();
            epilogue.Add(new BookText(BookUiText.Get("Epilogue"), 1.1f, Ink));
            string text = lore != null && !lore.Epilogue.IsEmpty ? BookLore.Text(lore.Epilogue, name) : BookUiText.Get("StoryDoneBody");
            epilogue.Add(new BookText(text, .82f, Ink));
            detail.Add(epilogue);
        }
        else if (step != null)
        {
            var objective = new BookSection();
            // Reference layout: chapter title with a large glyph, the stage description,
            // then framed sub-cards for the objective (slot + text + bar + "N / M") and the performer.
            var titleRow = new BookChapterTitle(TargetItem(step), step.TargetNpcType, step.Title,
                BookUiText.Format("StageOf", stageIndex + 1, PersonalResidentQuestCatalog.Length(selected), BookUiText.Get("Kinds." + step.Kind)));
            objective.Add(titleRow);
            if (loreStage != null && !loreStage.Description.IsEmpty) objective.Add(new BookText(BookLore.Text(loreStage.Description, name), .8f, Ink));
            string speech = !p.Active && loreStage != null ? BookLore.Text(loreStage.Offer, name)
                : p.Active && step.Kind == "Delayed" && loreStage != null ? BookLore.Text(loreStage.Waiting, name) : string.Empty;
            if (speech.Length > 0) objective.Add(new BookText(BookUiText.Format("QuoteFormat", speech), .76f, Muted));
            int objectiveProgress = d.Kind is ResidentQuestKind.DeliverSupplies or ResidentQuestKind.Expedition ? NpcQuestSystem.QuestInventoryCount(viewer, d, p) : p.Progress;
            float fraction = p.Active ? Math.Min(1, objectiveProgress / (float)Math.Max(1, d.Goal)) : 0;
            objective.Add(new BookSlotRow(new QuestTargetIcon(TargetItem(step), step.TargetNpcType, 34), ObjectiveLabel(npc, step, stageIndex, p),
                .88f, null, fraction, CountText(step, d, p.Active ? objectiveProgress : 0)));
            // Owner, 30.10: at 45/45 nothing said what comes next.
            if (p.Active && fraction >= 1f)
                objective.Add(new BookText(BookUiText.Format("Hints.ReadyTurnIn", name), .82f, new Color(46, 112, 40)));
            string chosen = BookQuestBridge.Choice(selected, stageIndex, p);
            if (chosen != null && step.Kind == "DialogueChoice")
            {
                string outcome = loreStage?.Choice(chosen) is { } c ? BookLore.Text(c.Outcome, name) : string.Empty;
                if (outcome.Length > 0) objective.Add(new BookText(outcome, .78f, Muted));
            }
            objective.Add(PerformerRow(selected, p, viewer));
            detail.Add(objective);
            detail.Add(RewardSection(d, p));
        }
        var toggle = new BookActionButton(BookUiText.Get(historyOpen ? "History.Hide" : "History.Show"), .8f);
        toggle.Width.Set(0, 1); toggle.Height.Set(38, 0);
        toggle.OnLeftClick += (_, _) => { historyOpen = !historyOpen; SoundEngine.PlaySound(SoundID.MenuTick); RebuildDetail(); Recalculate(); };
        detail.Add(toggle);
        if (historyOpen) detail.Add(HistorySection(npc, p, lore, name));
        var history = NpcQuestWorldLedger.History.Where(h => h.NpcKey == selected).TakeLast(5).Reverse().ToArray();
        if (history.Length > 0)
        {
            AddText(BookUiText.Get("Log.Title"), .9f);
            int length = PersonalResidentQuestCatalog.Length(selected);
            foreach (var h in history) AddText(BookUiText.Format("Log.Entry", Math.Min(h.Ordinal, length), length, h.Performer), .76f, true);
        }
        BuildFooter(npc, p, pending, complete, percent, step);
        Recalculate(); detail.ViewPosition = position;
    }

    private static int TargetItem(ResidentStoryStep step) => step.ItemType > 0 ? step.ItemType : step.TargetNpcType == 0 ? ItemID.Compass : 0;

    private static string ShortKey(string key) => key[(key.IndexOf('/') + 1)..];

    private static string EncounterHint(string key) => BookUiText.Optional("Encounter." + ShortKey(key)) is { Length: > 0 } text ? text : BookUiText.Get("Encounter.Default");

    /// <summary>Objective without numbers (they sit right of the bar), resolved in the client's language.</summary>
    private static string ObjectiveLabel(NpcTownIdentity npc, ResidentStoryStep step, int stageIndex, ResidentQuestProgress p)
    {
        string target = PersonalResidentQuestCatalog.Resolve(npc, stageIndex).TargetName;
        if (step.Kind == "DialogueChoice")
        {
            string chosen = BookQuestBridge.Choice(npc.NpcKey, stageIndex, p);
            foreach (var choice in step.Choices)
                if (choice.Id == chosen) return BookUiText.Format("Objective.DialogueChosen", choice.Text);
            return BookUiText.Get("Objective.DialogueChoice");
        }
        if (step.Kind == "Trial") return BookUiText.Format("Objective.Trial", target, Lang.GetBuffName(step.BuffType));
        if (step.Kind == "Delayed") return BookUiText.Get("Objective.Delayed");
        string count = step.Kind is "Deliver" or "Expedition" or "Hunt" or "GatherEnv" ? step.Count.ToString() : string.Empty;
        return BookUiText.Format("Objective." + step.Kind, target, count);
    }

    internal static string CountText(ResidentStoryStep step, ResidentQuestDefinition d, int progress)
    {
        int now = Math.Min(Math.Max(0, progress), d.Goal);
        return step.Kind switch
        {
            "Observe" or "Visit" => BookUiText.Format("CountSeconds", now / 60, d.Goal / 60),
            "Delayed" => BookUiText.Format("CountDays", now / PersonalResidentQuestCatalog.TicksPerDay, d.Goal / PersonalResidentQuestCatalog.TicksPerDay),
            _ => BookUiText.Format("Count", now, d.Goal)
        };
    }

    private static UIElement PerformerRow(string npcKey, ResidentQuestProgress p, Player viewer)
    {
        string me = viewer.GetModPlayer<NpcQuestPlayer>().PersistentId;
        // The head is looked up by the persistent quest identity first, the name only as a fallback (item 8 review).
        UIElement icon = p.Active ? new BookPlayerHead(BookPlayerHead.Find(p.AssignedTo, p.AssignedName)) : new QuestTargetIcon(ItemID.Compass, 0, 30);
        string text = !p.Active ? BookUiText.Get("Performer.None")
            : p.IsAssignedTo(me) ? BookUiText.Get("Performer.You")
            : BookQuestBridge.Paused(npcKey) ? BookUiText.Format("Performer.Paused", p.AssignedName)
            : BookUiText.Format("Performer.Other", p.AssignedName);
        return new BookSlotRow(icon, text, .88f, BookUiText.Get("Performer.Note"));
    }

    private static UIElement RewardSection(ResidentQuestDefinition d, ResidentQuestProgress p)
    {
        var rewards = new BookSection(88); rewards.Add(new BookText(BookUiText.Get("Rewards"), .89f, Ink));
        var rewardRow = new UIElement(); rewardRow.Width.Set(0, 1); rewardRow.Height.Set(45, 0);
        var reward = new QuestTargetIcon(d.RewardItemType, 0, 36); reward.Width.Set(38, 0); reward.Height.Set(38, 0); rewardRow.Append(reward);
        var itemLabel = new BookText(BookUiText.Format("RewardItem", d.RewardItemCount, Lang.GetItemNameValue(d.RewardItemType)), .83f, Ink); itemLabel.Left.Set(44, 0); itemLabel.Width.Set(-48, .60f); rewardRow.Append(itemLabel);
        int copper = p.Active ? p.RewardCopper : d.BaseRewardCopper;
        var coin = new QuestTargetIcon(copper >= 10000 ? ItemID.GoldCoin : ItemID.SilverCoin, 0, 30); coin.Left.Set(0, .61f); coin.Width.Set(32, 0); coin.Height.Set(38, 0); rewardRow.Append(coin);
        var coinLabel = new BookText(QuestRewardBalance.FormatCoins(copper), .84f, Ink); coinLabel.Left.Set(34, .61f); coinLabel.Width.Set(-34, .39f); rewardRow.Append(coinLabel);
        rewards.Add(rewardRow);
        return rewards;
    }

    /// <summary>"Вся история": the intro and all stages; descriptions and choice outcomes only for reached stages.</summary>
    private UIElement HistorySection(NpcTownIdentity npc, ResidentQuestProgress p, BookLoreResident lore, string name)
    {
        var section = new BookSection();
        section.Add(new BookText(BookUiText.Get("History.Title"), 1.02f, Ink));
        if (lore != null && !lore.Intro.IsEmpty) section.Add(new BookText(BookLore.Text(lore.Intro, name), .78f, Ink));
        int length = PersonalResidentQuestCatalog.Length(npc.NpcKey);
        for (int i = 0; i < length; i++)
        {
            var step = PersonalResidentQuestCatalog.Step(npc.NpcKey + ":" + i);
            if (step == null) continue;
            bool done = i < p.Completed, current = i == p.Completed;
            string state = BookUiText.Get(done ? "History.Done" : current ? "History.Current" : "History.Future");
            section.Add(new BookText(BookUiText.Format("History.StageLine", i + 1, step.Title, state), .84f, done || current ? Ink : Muted));
            BookLoreStage loreStage = lore?.Stage(i);
            if ((done || current) && loreStage != null && !loreStage.Description.IsEmpty)
                section.Add(new BookText(BookLore.Text(loreStage.Description, name), .74f, Muted));
            if (!done) continue;
            if (step.Kind == "DialogueChoice")
            {
                string chosen = BookQuestBridge.Choice(npc.NpcKey, i, p);
                string choiceText = step.Choices.FirstOrDefault(x => x.Id == chosen)?.Text;
                section.Add(new BookText(choiceText != null ? BookUiText.Format("History.Choice", choiceText) : BookUiText.Get("History.ChoiceUnknown"), .74f, Ink));
                string outcome = loreStage?.Choice(chosen) is { } c ? BookLore.Text(c.Outcome, name) : string.Empty;
                if (outcome.Length > 0) section.Add(new BookText(outcome, .74f, Muted));
            }
            if (loreStage != null && !loreStage.Complete.IsEmpty)
                section.Add(new BookText(BookUiText.Format("QuoteFormat", BookLore.Text(loreStage.Complete, name)), .74f, Muted));
        }
        return section;
    }

    private static int NearbyResident(string key)
    {
        foreach (var npc in Main.ActiveNPCs)
            if (NpcResidentRegistry.TryGet(npc, out string k, out _) && k == key && Vector2.DistanceSquared(npc.Center, Main.LocalPlayer.Center) <= 224 * 224) return npc.whoAmI;
        return -1;
    }

    private void AddText(string value, float scale, bool muted = false)
    {
        if (string.IsNullOrEmpty(value)) return;
        detail.Add(new BookText(value, scale, muted ? Muted : Ink));
    }

    private static BookActionButton Steel(string label, float scale, bool available)
    {
        var button = new BookActionButton(label, scale) { UseSteel = true, Available = available, TextColor = available ? Color.White : new Color(206, 202, 190) };
        button.Height.Set(44, 0);
        return button;
    }

    private void BuildFooter(NpcTownIdentity npc, ResidentQuestProgress p, bool pending, bool complete, int percent, ResidentStoryStep step)
    {
        questFooter.RemoveAllChildren();
        var questPlayer = Main.LocalPlayer.GetModPlayer<NpcQuestPlayer>();
        bool moved = NpcResidenceWorldSystem.IsOriginReleased(selected);
        // A paused stage (its performer left) can be continued by whoever speaks to the resident.
        bool paused = p.Active && p.Paused;
        bool mine = !p.Active || paused || p.IsAssignedTo(questPlayer.PersistentId);
        bool assignedToMe = p.Active && (p.IsAssignedTo(questPlayer.PersistentId) || p.IsPausedFor(questPlayer.PersistentId));
        var d = p.Active ? p.Definition(npc) : PersonalResidentQuestCatalog.Resolve(npc, p.Completed);
        bool ready = p.Active && !paused && NpcQuestSystem.IsReady(Main.LocalPlayer, d, p);
        int near = NearbyResident(selected); bool invite = complete && percent == 100 && !moved && !pending;
        string label = BookUiText.Get(invite ? "Buttons.Invite" : complete ? "Buttons.StoryDone" : ready ? "Buttons.TurnIn" : paused ? "Buttons.Resume" : p.Active ? "Buttons.InProgress" : "Buttons.Accept");
        // Reference: away from the resident the disabled button itself says "... при встрече".
        if (near < 0 && !pending && mine) label = invite ? BookUiText.Get("InviteOnMeet") : complete ? label : ready ? BookUiText.Get("TurnInOnMeet") : p.Active ? label : BookUiText.Get("AcceptOnMeet");
        bool enabled = near >= 0 && !pending && (invite || !complete && mine && (!p.Active || ready || paused));
        var tracker = Main.LocalPlayer.GetModPlayer<BookTrackerPlayer>();
        bool canTrack = !complete;
        bool canAbandon = assignedToMe && BookQuestBridge.CanAbandon;
        if (confirmAbandon && canAbandon)
        {
            // Fixed rows inside the 68 px above the story trail: a one-line question (0-18),
            // the two buttons (20-50) and a one-line consequence (51-67). Single-line texts
            // never wrap under the buttons; on a very narrow book they end with "...".
            var question = new BookText(BookUiText.Format("Hints.AbandonQuestion", step?.Title ?? string.Empty), .68f, Ink) { SingleLine = true };
            question.Left.Set(4, 0); question.Width.Set(-8, 1);
            questFooter.Append(question);
            var consequence = new BookText(BookUiText.Get("Hints.AbandonDetail"), .64f, Muted) { SingleLine = true };
            consequence.Top.Set(51, 0); consequence.Left.Set(4, 0); consequence.Width.Set(-8, 1);
            questFooter.Append(consequence);
            var yes = Steel(BookUiText.Get("Buttons.ConfirmAbandon"), .8f, true); yes.Top.Set(20, 0); yes.Left.Set(8, 0); yes.Width.Set(-12, .5f); yes.Height.Set(30, 0);
            yes.OnLeftClick += (_, _) => { confirmAbandon = false; SoundEngine.PlaySound(SoundID.MenuClose); BookQuestBridge.RequestAbandon?.Invoke(selected); Reload(); };
            var no = Steel(BookUiText.Get("Buttons.Cancel"), .8f, true); no.Top.Set(20, 0); no.Left.Set(4, .5f); no.Width.Set(-12, .5f); no.Height.Set(30, 0);
            no.OnLeftClick += (_, _) => { confirmAbandon = false; SoundEngine.PlaySound(SoundID.MenuTick); Reload(); };
            questFooter.Append(yes); questFooter.Append(no);
        }
        else if (p.Active && mine && !paused && !ready && d.Kind == ResidentQuestKind.DialogueChoice && p.Progress == 0 && step != null && step.Choices.Length >= 2)
        {
            // Dialogue-Choice: the authored options replace the single action button.
            int count = step.Choices.Length; float slot = 1f / count; bool near0 = near >= 0 && !pending;
            for (int i = 0; i < count; i++)
            {
                int index = i; var option = Steel(step.Choices[i].Text, .74f, near0);
                option.Left.Set(8 + 2 * i, slot * i); option.Width.Set(-16 * slot - 2, slot);
                option.OnLeftClick += (_, _) => { if (near0) { NpcQuestNetwork.RequestQuestChoice(near, index); Reload(); } };
                questFooter.Append(option);
            }
        }
        else
        {
            float secondary = (canTrack ? 1 : 0) + (canAbandon ? 1 : 0);
            float mainShare = secondary == 0 ? 1f : secondary == 1 ? .66f : .52f;
            var action = Steel(label, .9f, enabled);
            action.Left.Set(8, 0); action.Width.Set(-12, mainShare);
            action.OnLeftClick += (_, _) => { if (enabled) { NpcQuestNetwork.RequestQuestInteraction(near, invite); Reload(); } };
            questFooter.Append(action);
            float left = mainShare, share = secondary > 0 ? (1f - mainShare) / secondary : 0;
            if (canTrack)
            {
                bool tracked = tracker.IsTracked(selected);
                var track = new BookActionButton(BookUiText.Get(tracked ? "Buttons.Untrack" : "Buttons.Track"), .72f) { Pressed = tracked };
                track.Left.Set(2, left); track.Width.Set(-6, share); track.Height.Set(44, 0);
                track.OnLeftClick += (_, _) =>
                {
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    notice = tracker.ToggleTracked(selected) ? string.Empty : BookUiText.Get("Hints.TrackerFull");
                    Reload();
                };
                questFooter.Append(track); left += share;
            }
            if (canAbandon)
            {
                var abandon = new BookActionButton(BookUiText.Get("Buttons.Abandon"), .72f) { Warning = true };
                abandon.Left.Set(2, left); abandon.Width.Set(-6, share); abandon.Height.Set(44, 0);
                abandon.OnLeftClick += (_, _) => { confirmAbandon = true; SoundEngine.PlaySound(SoundID.MenuTick); Reload(); };
                questFooter.Append(abandon);
            }
        }
        string hint = notice.Length > 0 ? notice
            : pending ? BookUiText.Get("Hints.FindFirst")
            : paused ? BookUiText.Get("Hints.Paused")
            : !mine ? BookUiText.Format("Hints.AssignedTo", p.AssignedName)
            : ready && near < 0 ? BookUiText.Format("Hints.ReadyTurnIn", NpcQuestSystem.ResidentName(selected))
            : near < 0 && p.Active && !ready && !complete ? BookUiText.Get("Hints.MeetToSpeak") : string.Empty;
        if (!confirmAbandon)
        {
            var hintText = new BookText(hint, .67f, Muted) { SingleLine = true }; hintText.Top.Set(47, 0); hintText.Left.Set(4, 0); hintText.Width.Set(-8, 1); questFooter.Append(hintText);
        }
        // Reference: the chain sits in its own framed card; the residence lock gets a full caption beside it.
        var trail = new ResidentStoryTrail(selected, p.Completed); trail.Top.Set(68, 0); trail.Width.Set(-170, 1); trail.Height.Set(104, 0); questFooter.Append(trail);
        var house = new BookAssetIcon(moved ? BookGlyph.Residents : BookGlyph.Residence); house.Left.Set(-160, 1); house.Top.Set(62, 0); house.Width.Set(150, 0); house.Height.Set(40, 0); questFooter.Append(house);
        // S23: residences are no longer locked; the caption shows the reader's standing with this resident instead.
        int standing = ResidentReputationSystem.Local(selected);
        string mood = standing <= ResidentReputationSystem.HostileAt ? "Hostile" : standing <= ResidentReputationSystem.QuestBanAt ? "Banned"
            : standing <= ResidentReputationSystem.GrumbleAt ? "Grumbling" : standing >= 700 ? "Devoted" : standing >= 300 ? "Friendly" : "Neutral";
        string caption = moved ? BookUiText.Get("ResidenceMoved")
            : Language.GetTextValue("Mods.DingDongBiomeHomes.Reputation.Line", standing) + " · " + Language.GetTextValue("Mods.DingDongBiomeHomes.Reputation.Mood." + mood);
        var homeText = new BookText(caption, .62f, standing <= ResidentReputationSystem.QuestBanAt ? new Color(150, 40, 30) : Ink) { Centered = true }; homeText.Left.Set(-162, 1); homeText.Top.Set(102, 0); homeText.Width.Set(156, 0); questFooter.Append(homeText);
        var (brokenCount, brokenPrice) = ResidentReputationSystem.LocalBroken(selected);
        var entries = ResidentReputationSystem.LocalEntries(selected);
        if (!moved && brokenCount > 0)
        {
            // One broken block at a time (‹ › pick, the middle button pays for it), or everything at once below.
            if (entries.Count > 0)
            {
                buybackCursor = Math.Clamp(buybackCursor, 0, entries.Count - 1);
                var (entryId, entryItem, entryPrice) = entries[buybackCursor];
                string one = QuestRewardBalance.FormatCoins((int)Math.Min(int.MaxValue, entryPrice));
                var prev = new BookActionButton("‹", .7f) { Available = buybackCursor > 0 };
                prev.Left.Set(-162, 1); prev.Top.Set(120, 0); prev.Width.Set(24, 0); prev.Height.Set(24, 0);
                prev.OnLeftClick += (_, _) => { if (buybackCursor > 0) { buybackCursor--; SoundEngine.PlaySound(SoundID.MenuTick); Reload(); } };
                var next = new BookActionButton("›", .7f) { Available = buybackCursor < entries.Count - 1 };
                next.Left.Set(-30, 1); next.Top.Set(120, 0); next.Width.Set(24, 0); next.Height.Set(24, 0);
                next.OnLeftClick += (_, _) => { if (buybackCursor < entries.Count - 1) { buybackCursor++; SoundEngine.PlaySound(SoundID.MenuTick); Reload(); } };
                string blockName = entryItem > 0 ? Lang.GetItemNameValue(entryItem) : "?";
                var buyOne = new BookActionButton(Language.GetTextValue("Mods.DingDongBiomeHomes.Reputation.BuybackOne", blockName, one), .5f) { Warning = !Main.LocalPlayer.CanAfford(entryPrice) };
                buyOne.Left.Set(-136, 1); buyOne.Top.Set(120, 0); buyOne.Width.Set(104, 0); buyOne.Height.Set(24, 0);
                buyOne.OnLeftClick += (_, _) =>
                {
                    SoundEngine.PlaySound(SoundID.Coins);
                    notice = ResidentReputationSystem.RequestBuyback(selected, entryId) ? string.Empty : Language.GetTextValue("Mods.DingDongBiomeHomes.Reputation.BuybackPoor", one);
                    Reload();
                };
                questFooter.Append(prev); questFooter.Append(buyOne); questFooter.Append(next);
            }
            string coins = QuestRewardBalance.FormatCoins((int)Math.Min(int.MaxValue, brokenPrice));
            var buyback = new BookActionButton(Language.GetTextValue("Mods.DingDongBiomeHomes.Reputation.BuybackAll", brokenCount, coins), .55f) { Warning = !Main.LocalPlayer.CanAfford(brokenPrice) };
            buyback.Left.Set(-162, 1); buyback.Top.Set(147, 0); buyback.Width.Set(156, 0); buyback.Height.Set(25, 0);
            buyback.OnLeftClick += (_, _) =>
            {
                SoundEngine.PlaySound(SoundID.Coins);
                notice = ResidentReputationSystem.RequestBuyback(selected) ? string.Empty : Language.GetTextValue("Mods.DingDongBiomeHomes.Reputation.BuybackPoor", coins);
                Reload();
            };
            questFooter.Append(buyback);
        }
    }
}

internal sealed class BookText : UIElement
{
    private const string Ellipsis = "...";
    private string text; private readonly float scale; private float drawScale; private bool clipped; private string[] lines = Array.Empty<string>(); private float[] widths = Array.Empty<float>();
    internal Color TextColor { get; set; }
    internal bool Centered { get; set; }
    /// <summary>One line only: a text that does not fit is cut at a word (or letter) and ends with "...".</summary>
    internal bool SingleLine { get; set; }
    // V03 28.10: a single line first shrinks down to MinScale, only then is cut; a cut line shows the full text on hover.
    internal float MinScale { get; set; }
    internal bool Clipped => clipped;
    internal string Value => text;
    internal BookText(string text, float scale, Color color)
    { this.text = text ?? string.Empty; this.scale = scale; drawScale = scale; TextColor = color; Width.Set(0, 1); Height.Set(24, 0); IgnoresMouseInteraction = true; }
    internal void SetText(string value) { text = value ?? string.Empty; Recalculate(); }
    private static float Measure(string value, float scale) => FontAssets.MouseText.Value.MeasureString(value).X * scale;
    public override void Recalculate()
    {
        base.Recalculate(); float width = Math.Max(20, GetInnerDimensions().Width); var output = new List<string>(); drawScale = scale;
        if (SingleLine)
        {
            // V03 28.10: owner shot 18 - long names must fit their row: shrink, then cut, full text on hover.
            string line = text.Replace('\n', ' '); float full = Measure(line, 1f); bool cutLine = false;
            if (full * scale > width)
            {
                float floor = MinScale > 0 ? Math.Min(MinScale, scale) : scale;
                drawScale = Math.Max(floor, width / Math.Max(1f, full));
                if (full * drawScale > width + .5f)
                {
                    int lo = 0, hi = line.Length; // longest prefix that fits with the ellipsis at the floor scale
                    while (lo < hi) { int mid = (lo + hi + 1) / 2; if (Measure(line[..mid].TrimEnd() + Ellipsis, drawScale) <= width) lo = mid; else hi = mid - 1; }
                    int space = line.LastIndexOf(' ', Math.Max(0, lo - 1));
                    if (space > lo / 2) lo = space;
                    line = line[..lo].TrimEnd().TrimEnd('-', ',') + Ellipsis; cutLine = true;
                }
            }
            clipped = cutLine; IgnoresMouseInteraction = !cutLine; // only a cut line takes the hover; clicks bubble to the row
            output.Add(line);
        }
        else
            foreach (var paragraph in text.Split('\n'))
            {
                string line = "";
                foreach (string originalWord in paragraph.Split(' '))
                {
                    string word = originalWord;
                    if (line.Length > 0 && Measure(line + " " + word, scale) > width) { output.Add(line); line = ""; }
                    // Only a single word wider than the whole column is split.
                    while (word.Length > 1 && Measure(word, scale) > width)
                    {
                        int split = 1; while (split < word.Length && Measure(word[..(split + 1)], scale) <= width) split++;
                        output.Add(word[..split]); word = word[split..];
                    }
                    line = line.Length == 0 ? word : line + " " + word;
                }
                output.Add(line);
            }
        lines = output.ToArray(); widths = new float[lines.Length];
        for (int i = 0; i < lines.Length; i++) widths[i] = Measure(lines[i], drawScale);
        Height.Set(Math.Max(1, lines.Length) * FontAssets.MouseText.Value.LineSpacing * scale + 2, 0); base.Recalculate();
    }
    protected override void DrawSelf(SpriteBatch b)
    {
        var inner = GetInnerDimensions(); var p = inner.Position(); float lineSpacing = FontAssets.MouseText.Value.LineSpacing;
        p.Y += (scale - drawScale) * lineSpacing * .5f; // V03 28.10: a shrunk line stays centred in its slot
        for (int i = 0; i < lines.Length; i++)
        {
            var at = Centered && i < widths.Length ? new Vector2(p.X + Math.Max(0, (inner.Width - widths[i]) * .5f), p.Y) : p;
            b.DrawString(FontAssets.MouseText.Value, lines[i], at, TextColor, 0, Vector2.Zero, drawScale, SpriteEffects.None, 0); p.Y += lineSpacing * drawScale;
        }
        if (clipped && IsMouseHovering) Main.hoverItemName = text;
    }
}

internal sealed class BookProgressBar : UIElement
{
    private readonly float value;
    internal BookProgressBar(float value) { this.value = Math.Clamp(value, 0, 1); Width.Set(0, 1); Height.Set(12, 0); IgnoresMouseInteraction = true; }
    protected override void DrawSelf(SpriteBatch b)
    { var a = GetDimensions().ToRectangle(); DingDongUiTheme.DrawPanel(b, a, new Color(112, 100, 76), new Color(71, 52, 29)); a.Inflate(-2, -2); a.Width = (int)(a.Width * value); DingDongUiTheme.Fill(b, a, new Color(89, 147, 49)); }
}

internal sealed class BookResidentRow : UIElement
{
    internal const float RowHeight = 98;
    private readonly bool selected, mainStory;
    private readonly QuestTargetIcon portrait;
    private readonly BookText title, amount, status;
    private readonly BookProgressBar bar;
    private readonly BookItemGlyph role, star;
    private readonly float amountWidth;
    // Reference layout (owner item 8): portrait | name, reputation bar and "40 / 100" | role glyph above the status.
    internal BookResidentRow(string name, int reputation, int npcType, bool selected, BookQuestStatus state, string stateText, string stateHover, bool mainStory, string npcKey, string roleHover)
    {
        this.selected = selected; this.mainStory = mainStory; Width.Set(0, 1); Height.Set(RowHeight, 0); SetPadding(7);
        portrait = new QuestTargetIcon(0, npcType, 82, true); portrait.Width.Set(66, 0); portrait.Height.Set(76, 0); Append(portrait);
        // Long names are cut with "..." instead of running under the status column (item 8 review).
        title = new BookText(name, 1.04f, ResidentBookJournal.Ink) { SingleLine = true, MinScale = .8f }; Append(title);
        if (mainStory)
        {
            // Main storyline marker (owner item 11): a fallen star before the name, explained on hover.
            star = new BookItemGlyph(ItemID.FallenStar, 22, BookUiText.Get("MainStory"), Color.White); Append(star);
        }
        bar = new BookProgressBar((reputation - ResidentReputationSystem.Min) / (float)(ResidentReputationSystem.Max - ResidentReputationSystem.Min)); bar.Height.Set(14, 0); Append(bar);
        string amountText = reputation.ToString();
        // Measured, so "100 / 100" never overflows a fixed column (item 8 review).
        amountWidth = FontAssets.MouseText.Value.MeasureString(BookUiText.Format("Count", 100, 100)).X * .86f + 6f;
        amount = new BookText(amountText, .86f, ResidentBookJournal.Ink) { SingleLine = true, MinScale = .6f }; Append(amount);
        bool dim = state == BookQuestStatus.Find;
        int roleItem = ResidentRoleIcons.Get(npcKey, npcKey == null ? "" : PersonalResidentQuestCatalog.Force(npcKey));
        // The glyph explains the resident's role and force; the status has its own caption (item 8 review).
        role = new BookItemGlyph(roleItem, 30, roleHover, dim ? new Color(120, 112, 98) : Color.White); Append(role);
        status = new BookText(stateText, .72f, ResidentBookJournal.StatusColor(state)) { Centered = true, SingleLine = true, MinScale = .5f }; Append(status);
        statusHover = stateHover;
    }
    private readonly string statusHover;
    public override void Recalculate()
    {
        base.Recalculate();
        float width = GetInnerDimensions().Width, column = Math.Clamp(width * .27f, 80, 120), left = 76, nameLeft = mainStory ? left + 26 : left;
        if (star != null) { star.Left.Set(left, 0); star.Top.Set(3, 0); }
        title.Left.Set(nameLeft, 0); title.Top.Set(2, 0); title.Width.Set(-nameLeft - column - 4, 1);
        bar.Left.Set(left, 0); bar.Top.Set(40, 0); bar.Width.Set(-left - column - amountWidth - 10, 1);
        amount.Left.Set(-column - amountWidth - 2, 1); amount.Top.Set(35, 0); amount.Width.Set(amountWidth, 0);
        role.Left.Set(-column + (column - 30) * .5f, 1); role.Top.Set(8, 0); role.Width.Set(30, 0); role.Height.Set(30, 0);
        status.Left.Set(-column, 1); status.Top.Set(46, 0); status.Width.Set(column, 0);
        base.Recalculate();
    }
    protected override void DrawSelf(SpriteBatch b)
    {
        var area = GetDimensions().ToRectangle();
        DingDongUiAssets.DrawBookCard(b, area, selected ? new Color(255, 236, 177) : IsMouseHovering ? Color.White : Color.White * .72f);
        // Main storyline rows carry a gold frame and spine so they stand out while scrolling (owner item 11).
        if (mainStory) { BookFrames.Gold(b, area); DingDongUiTheme.Fill(b, new Rectangle(area.X + 5, area.Y + 12, 3, Math.Max(0, area.Height - 24)), new Color(196, 146, 48)); }
        if (IsMouseHovering && !role.IsMouseHovering && Main.MouseScreen.X >= status.GetDimensions().X && Main.MouseScreen.Y >= status.GetDimensions().Y - 4) Main.hoverItemName = statusHover;
    }
}

internal sealed class BookSection : JournalFrame
{
    private readonly List<UIElement> rows = new();
    private readonly float minimumHeight;
    internal BookSection(float minimumHeight = 0) { this.minimumHeight = minimumHeight; Width.Set(0, 1); Height.Set(Math.Max(80, minimumHeight), 0); SetPadding(12); }
    internal void Add(UIElement row) { rows.Add(row); Append(row); }
    public override void Recalculate()
    {
        base.Recalculate(); float y = 0;
        foreach (var row in rows) { row.Top.Set(y, 0); row.Recalculate(); y += row.GetOuterDimensions().Height + 8; }
        Height.Set(Math.Max(minimumHeight, Math.Max(0, y - 8) + PaddingTop + PaddingBottom), 0); base.Recalculate();
    }
}

internal sealed class BookActionButton : UITextPanel<string>
{
    private readonly float scale;
    private float fitScale;
    private Vector2 size;
    internal bool UseSteel { get; set; }
    internal bool Available { get; set; } = true;
    /// <summary>Toggle buttons ("Отслеживать") show a pressed parchment while active.</summary>
    internal bool Pressed { get; set; }
    /// <summary>Destructive actions ("Отказаться") get a red-brown caption.</summary>
    internal bool Warning { get; set; }
    internal BookActionButton(string text, float scale) : base(text, scale) { this.scale = scale; fitScale = scale; }
    public override void Recalculate()
    {
        // UITextPanel sets MinWidth to the caption width, which pushed long captions past their
        // share of the footer row (owner shot 29.10): the caption shrinks instead.
        MinWidth.Set(0, 0); MinHeight.Set(0, 0);
        base.Recalculate();
        // Fit the caption once per layout (no measuring while drawing).
        var font = FontAssets.MouseText.Value; float measured = Math.Max(1f, font.MeasureString(Text ?? string.Empty).X);
        fitScale = Math.Clamp((GetDimensions().Width - 18f) / measured, .42f, scale);
        size = font.MeasureString(Text ?? string.Empty) * fitScale;
    }
    protected override void DrawSelf(SpriteBatch batch)
    {
        var area = GetDimensions().ToRectangle();
        if (UseSteel) BookSkin.ActionFrame(batch, area, IsMouseHovering && Available ? new Color(255, 236, 190) : Color.White * (Available ? 1f : .76f));
        else DingDongUiAssets.DrawBookCard(batch, area, Pressed ? new Color(247, 214, 140) : IsMouseHovering ? new Color(255, 239, 193) : BackgroundColor);
        Color color = UseSteel ? TextColor : Warning ? new Color(137, 52, 29) : ResidentBookJournal.Ink;
        batch.DrawString(FontAssets.MouseText.Value, Text, area.Center.ToVector2() - size * .5f, color, 0, Vector2.Zero, fitScale, SpriteEffects.None, 0);
    }
}
