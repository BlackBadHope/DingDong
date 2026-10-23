using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
/// <summary>
/// Applies deterministic client prediction and validates the same decision on
/// the server before the vanilla DamageNPC packet is allowed to mutate life.
/// </summary>
public sealed partial class DamageAuthoritySystem
{
	public override void Load() => On_NetMessage.SendStrikeNPC += CaptureOutgoingStrike;
	public override void Unload() => On_NetMessage.SendStrikeNPC -= CaptureOutgoingStrike;
	public override void OnWorldLoad() => ResetSession();
	public override void OnWorldUnload() => ResetSession();
}
