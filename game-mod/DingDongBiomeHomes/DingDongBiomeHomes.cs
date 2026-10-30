using System.IO;
using DingDongBiomeHomes.Items.AccessoryEnchanting;
using DingDongBiomeHomes.Items.MobRelics;
using DingDongBiomeHomes.Items.SynergyArmor;
using DingDongBiomeHomes.Worldgen;
using DingDongBiomeHomes.Worldgen.Compiler.Api;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

public sealed class DingDongBiomeHomes : Mod
{
	internal static DingDongBiomeHomes Instance { get; private set; }
	internal static ModKeybind OpenDamageGuideKeybind { get; private set; }
	internal static ModKeybind OpenQuestJournalKeybind { get; private set; }

	public override void Load()
	{
		Instance = this;
		ContinentalSectionPacketEnvelope.Install();
		ContinentalWorldHooks.Install();
		MobSpecificRelicCatalog.Register(this);
		SynergyArmorCatalog.Register(this);
		if (!Main.dedServ)
		{
			OpenDamageGuideKeybind = KeybindLoader.RegisterKeybind(this, "OpenDamageGuide", "J");
			OpenQuestJournalKeybind = KeybindLoader.RegisterKeybind(this, "OpenQuestJournal", "None");
			DingDongUiAssets.Load(this);
		}
		ServiceResidentShopIntegration.InstallHooks();
		ResidentHousingDoctrineHooks.Install();
	}

	public override void PostSetupContent() => DamageProtocolAutoProbe.TryRun(this);

	public override void Unload()
	{
		ContinentalSectionPacketEnvelope.Uninstall();
		ContinentalSectionStreamingNetwork.Reset();
		ContinentalWorldHooks.Uninstall();
		ResidentHousingDoctrineHooks.Uninstall();
		MobRelicGlobalNPC.Reset();
		UnifiedLootNormalization.Reset();
		MobSpecificRelicCatalog.Reset();
		SynergyArmorCatalog.Reset();
		DamageProtocolAutoProbe.Reset();
		DamageNameMatcher.Reset();
		NpcResidentRegistry.Reset();
		QuestContentRegistry.Reset();
		OpenDamageGuideKeybind = null;
		OpenQuestJournalKeybind = null;
		DingDongUiAssets.Unload();
		WorldCompilerExtensionRegistry.ResetAll();
		Instance = null;
	}

	public override object Call(params object[] args) =>
		DingDongModCallRouter.Route(args);

	public override void HandlePacket(BinaryReader reader, int whoAmI)
	{
		byte packetKind = reader.ReadByte();
		if (packetKind is Compatibility.AndroStorageNetwork.Request or Compatibility.AndroStorageNetwork.Snapshot)
		{
			Compatibility.AndroStorageNetwork.Handle(packetKind, reader, whoAmI);
			return;
		}
		if (DamageAuthoritySystem.IsPacketKind(packetKind))
		{
			DamageAuthoritySystem.HandlePacket(reader, whoAmI, (DamagePacketKind)packetKind);
			return;
		}
		if (AccessoryEnchantingNetwork.IsPacketKind(packetKind))
		{
			AccessoryEnchantingNetwork.HandlePacket(packetKind, reader, whoAmI);
			return;
		}
		if (ContinentalSectionStreamingNetwork.IsPacketKind(packetKind))
		{
			ContinentalSectionStreamingNetwork.HandlePacket(reader, whoAmI, packetKind);
			return;
		}
		if (Economy.ResidentPurseSystem.IsPacket(packetKind))
		{
			Economy.ResidentPurseSystem.HandlePacket(packetKind, reader, whoAmI);
			return;
		}
		if (ResidentReputationSystem.IsPacket(packetKind))
		{
			ResidentReputationSystem.HandlePacket(packetKind, reader, whoAmI);
			return;
		}
		if (packetKind is Fishing.FishingNetwork.Select or Fishing.FishingNetwork.Snapshot)
		{
			Fishing.FishingNetwork.Handle(packetKind, reader, whoAmI);
			return;
		}

		if (!System.Enum.IsDefined(typeof(DingDongPacketKind), packetKind))
		{
			ConsumeRemainingPacket(reader);
			PacketRejectGuard.Reject("UNKNOWN", whoAmI, packetKind, "UnknownPacketKind");
			return;
		}
		NpcQuestNetwork.HandlePacket((DingDongPacketKind)packetKind, reader, whoAmI);
	}

	internal static void ConsumeRemainingPacket(BinaryReader reader)
	{
		Stream stream = reader.BaseStream;
		if (stream.CanSeek)
		{
			// MessageBuffer reuses a larger stream for many network packets.
			// Only discard this frame, never the rest of the backing buffer.
			ContinentalSectionPacketEnvelope.Discard(reader);
			return;
		}

		while (stream.ReadByte() >= 0)
		{
		}
	}
}
