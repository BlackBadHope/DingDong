namespace DingDongBiomeHomes;

internal enum DingDongPacketKind : byte
{
	QuestInteractRequest = 64,
	QuestSnapshot = 65,
	FlagClaimRequest = 66,
	EscortSnapshot = 67,
	PlayerIdentityHello = 68,
	AccessoryStationSlotSync = 69,
	AccessoryStationActionRequest = 70,
	AccessoryStationActionResult = 71,
	AccessoryStationSlotSwapRequest = 72,
	SectionResendRequest = 73,
	SectionDeliveryProbe = 74,
	SectionDeliveryAck = 75,
	FishingRigSelect = 76,
	FishingRigSnapshot = 77,
    ResidentEncounterTalk = 80,
    ResidentEncounterSnapshot = 81,
    QuestChoiceRequest = 82
}
