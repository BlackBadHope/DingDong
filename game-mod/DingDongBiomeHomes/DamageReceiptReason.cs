using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
internal enum DamageReceiptReason : byte
{
	Accepted = 0,
	ProtocolMismatch = 1,
	InvalidSender = 2,
	InvalidTarget = 3,
	InvalidLineage = 4,
	SourceMismatch = 5,
	NonMonotonicTick = 6,
	MultiplierMismatch = 7,
	MissingApproval = 8,
	MalformedPacket = 9,
	ServerBusy = 10,
	CommitMismatch = 11,
	MissingCommit = 12,
	PayloadMismatch = 13,
	DuplicateCommit = 14
}
