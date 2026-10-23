using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
internal enum DamageApprovalResult
{
	Prepared,
	Committed,
	Consumed,
	Missing,
	Expired,
	Busy,
	IdentityMismatch,
	MissingCommit,
	PayloadMismatch,
	DuplicateCommit
}
