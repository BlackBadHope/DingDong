using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

internal enum QuestNoticeKind : byte
{
	Accepted = 1,
	Chosen = 2,
	Completed = 3,
	ObserveFailed = 4,
	TrialFailed = 5,
	Abandoned = 6,
	Resumed = 7,
	ObjectiveMissing = 8,
	ObjectiveReady = 9
}

/// <summary>
/// Story quest notices travel as keys (resident, stage, choice index, coins) and are
/// worded on the receiving client, so a Russian client on an English server reads
/// Russian titles, choices and resident lines (and vice versa).
/// </summary>
internal static class QuestNotice
{
	private static readonly Color NoticeColor = new(110, 190, 255);
	private static readonly Color SpeechColor = new(255, 222, 160);
	private static string pendingLine = "", pendingNpc = "";
	private static int pendingTicks;

	internal static void Send(Player player, QuestNoticeKind kind, string npcKey, int stage, int arg = 0)
	{
		if (player == null || string.IsNullOrEmpty(npcKey)) return;
		if (Main.netMode == NetmodeID.Server)
		{
			if (!NpcQuestNetwork.CanSendToClient(player.whoAmI)) return;
			ModPacket packet = DingDongBiomeHomes.Instance.GetPacket();
			packet.Write((byte)DingDongPacketKind.QuestNotice);
			Write(packet, kind, npcKey, stage, arg);
			packet.Send(player.whoAmI);
		}
		else if (Main.netMode == NetmodeID.SinglePlayer && player.whoAmI == Main.myPlayer && !Main.dedServ)
			Show(kind, npcKey, stage, arg);
	}

	private static void Write(BinaryWriter writer, QuestNoticeKind kind, string npcKey, int stage, int arg)
	{
		writer.Write((byte)kind); writer.Write(npcKey); writer.Write((byte)Math.Clamp(stage, 0, 255)); writer.Write(arg);
	}

	private static void Read(BinaryReader reader, out QuestNoticeKind kind, out string npcKey, out int stage, out int arg)
	{
		byte raw = reader.ReadByte(); npcKey = reader.ReadString(); stage = reader.ReadByte(); arg = reader.ReadInt32();
		if (!Enum.IsDefined(typeof(QuestNoticeKind), raw) || npcKey.Length == 0 || npcKey.Length > NpcQuestNetwork.MaximumQuestStringLength || !NpcTownManifest.TryGet(npcKey, out _))
			throw new InvalidDataException("Invalid quest notice.");
		kind = (QuestNoticeKind)raw;
	}

	internal static void Receive(BinaryReader reader)
	{
		Read(reader, out var kind, out string npcKey, out int stage, out int arg);
		Show(kind, npcKey, stage, arg);
	}

	internal static bool PacketContractPass()
	{
		using MemoryStream stream = new();
		using (BinaryWriter writer = new(stream, System.Text.Encoding.UTF8, leaveOpen: true))
			Write(writer, QuestNoticeKind.Chosen, "Terraria/Guide", 2, 1);
		stream.Position = 0;
		using BinaryReader reader = new(stream, System.Text.Encoding.UTF8, leaveOpen: true);
		Read(reader, out var kind, out string key, out int stage, out int arg);
		return kind == QuestNoticeKind.Chosen && key == "Terraria/Guide" && stage == 2 && arg == 1 && stream.Position == stream.Length;
	}

	private static void Show(QuestNoticeKind kind, string npcKey, int stage, int arg)
	{
		if (Main.dedServ) return;
		var step = PersonalResidentQuestCatalog.Step(npcKey, stage);
		string title = step?.Title ?? "";
		var tracker = Main.LocalPlayer.GetModPlayer<BookTrackerPlayer>();
		string line = "";
		switch (kind)
		{
			case QuestNoticeKind.Accepted:
				Say("Mods.DingDongBiomeHomes.SharedQuests.StoryAccepted", title);
				if (step != null) Say("Mods.DingDongBiomeHomes.Quests.RewardPromised", Bundle(step, arg));
				line = LoreCatalog.Offer(npcKey, stage);
				tracker.TrackIfRoom(npcKey);
				break;
			case QuestNoticeKind.Chosen:
				if (step != null && arg >= 0 && arg < step.Choices.Length)
				{
					Say("Mods.DingDongBiomeHomes.SharedQuests.Chosen", step.Choices[arg].Text);
					line = LoreCatalog.Outcome(npcKey, stage, step.Choices[arg].Id);
				}
				break;
			case QuestNoticeKind.Completed:
				if (step != null) Say("Mods.DingDongBiomeHomes.SharedQuests.RewardGranted", Bundle(step, arg));
				line = LoreCatalog.Complete(npcKey, stage);
				// Tracking survives between stages; the finished story leaves the tracker.
				if (step != null && PersonalResidentQuestCatalog.Complete(npcKey, stage + 1)) tracker.Untrack(npcKey);
				break;
			case QuestNoticeKind.ObserveFailed:
				Say("Mods.DingDongBiomeHomes.QuestEngine.Notice.ObserveFailed", title, step != null ? Lang.GetNPCNameValue(step.TargetNpcType) : "");
				break;
			case QuestNoticeKind.TrialFailed:
				Say("Mods.DingDongBiomeHomes.QuestEngine.Notice.TrialFailed", title, step != null ? Lang.GetBuffName(step.BuffType) : "");
				break;
			case QuestNoticeKind.Abandoned:
				Say("Mods.DingDongBiomeHomes.QuestEngine.Notice.Abandoned", title);
				tracker.Untrack(npcKey);
				break;
			case QuestNoticeKind.ObjectiveMissing:
				var local = Main.LocalPlayer.GetModPlayer<NpcQuestPlayer>();
				if (local.TryGetCurrent(npcKey, out var progress) && progress.Active && NpcTownManifest.TryGet(npcKey, out var identity))
					Say("Mods.DingDongBiomeHomes.Quests.ObjectiveMissing", NpcQuestSystem.ObjectiveText(Main.LocalPlayer, progress.Definition(identity), progress));
				break;
			case QuestNoticeKind.ObjectiveReady:
				Say("Mods.DingDongBiomeHomes.Quests.ObjectiveReady", NpcQuestSystem.ResidentName(npcKey));
				break;
			case QuestNoticeKind.Resumed:
				Say("Mods.DingDongBiomeHomes.QuestEngine.Notice.Resumed", title);
				tracker.TrackIfRoom(npcKey);
				break;
		}
		if (line.Length > 0) { pendingLine = line; pendingNpc = npcKey; pendingTicks = 2; }
		LoreCodex.Invalidate();
	}

	private static string Bundle(ResidentStoryStep step, int coins) =>
		Language.GetTextValue("Mods.DingDongBiomeHomes.QuestEngine.RewardBundle", QuestRewardBalance.FormatCoins(Math.Max(0, coins)), step.RewardCount, Lang.GetItemNameValue(step.RewardType));

	private static void Say(string key, params object[] args) => Main.NewText(Language.GetTextValue(key, args), NoticeColor.R, NoticeColor.G, NoticeColor.B);

	// Applied one update later: the chat-button handler writes its own "request sent"
	// text right after the (single-player) interaction, and the resident's reply wins.
	internal static void Update()
	{
		if (pendingLine.Length == 0 || --pendingTicks > 0) return;
		string line = pendingLine, npcKey = pendingNpc;
		pendingLine = pendingNpc = "";
		Player local = Main.LocalPlayer;
		if (local.talkNPC >= 0 && local.talkNPC < Main.maxNPCs && NpcResidentRegistry.TryGet(Main.npc[local.talkNPC], out string talking, out _) && talking == npcKey)
			Main.npcChatText = line;
		else
			Main.NewText(Language.GetTextValue("Mods.DingDongBiomeHomes.QuestEngine.Notice.Says", NpcQuestSystem.ResidentName(npcKey), line), SpeechColor.R, SpeechColor.G, SpeechColor.B);
	}

	internal static void Clear() { pendingLine = pendingNpc = ""; pendingTicks = 0; }
}

public sealed class QuestNoticeSystem : ModSystem
{
	public override void PostUpdateEverything() { if (!Main.dedServ) QuestNotice.Update(); }
	public override void OnWorldUnload() => QuestNotice.Clear();
}
