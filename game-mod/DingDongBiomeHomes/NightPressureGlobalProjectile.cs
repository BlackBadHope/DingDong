using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using System.IO;

namespace DingDongBiomeHomes;

public sealed class NightPressureGlobalProjectile : GlobalProjectile
{
	private bool hostileNpcSource;
	public override bool InstancePerEntity => true;

	public override void OnSpawn(Projectile projectile, IEntitySource source)
	{
		hostileNpcSource = source is EntitySource_Parent parent && parent.Entity is NPC npc && !npc.friendly && npc.damage > 0;
	}

	public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter) =>
		bitWriter.WriteBit(hostileNpcSource);
	public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader) =>
		hostileNpcSource = bitReader.ReadBit();

	public override void ModifyHitPlayer(Projectile projectile, Player target, ref Player.HurtModifiers modifiers)
	{
		if (hostileNpcSource && !Main.dayTime && ModContent.GetInstance<DingDongServerConfig>().EnableNightPressure)
			modifiers.SourceDamage *= NightPressureGlobalNPC.HostileContactDamageMultiplier;
	}
}
