using System;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;

public sealed class DamageCombatPlayer : ModPlayer
{
	private uint nextSequence;

	public ulong CurrentAttackInstanceId { get; private set; }

	public override void Initialize()
	{
		nextSequence = 0;
		CurrentAttackInstanceId = 0;
	}

	public override void OnEnterWorld()
	{
		nextSequence = 0;
		CurrentAttackInstanceId = 0;
	}

	public ulong BeginAttack()
	{
		nextSequence++;
		if (nextSequence == 0)
			nextSequence = 1;
		CurrentAttackInstanceId = Compose(Player.whoAmI, nextSequence);
		return CurrentAttackInstanceId;
	}

	public ulong EnsureAttack() => CurrentAttackInstanceId != 0 ? CurrentAttackInstanceId : BeginAttack();

	public static ulong Compose(int playerId, uint sequence)
	{
		if (playerId < 0 || playerId >= Main.maxPlayers || sequence == 0)
			return 0;
		return ((ulong)(playerId + 1) << 56) | sequence;
	}

	public static bool BelongsToPlayer(ulong attackInstanceId, int playerId) =>
		attackInstanceId != 0 && (byte)(attackInstanceId >> 56) == playerId + 1;
}

public sealed class DamageLineageGlobalProjectile : GlobalProjectile
{
	public override bool InstancePerEntity => true;

	public ulong AttackInstanceId { get; private set; }
	public int OriginItemType { get; private set; }
	public int AmmoItemType { get; private set; }
	public string AcquisitionTag { get; private set; } = DamageTags.UnspecifiedAcquisition;
	public string ElementTag { get; private set; } = DamageTags.Neutral;

	public override void OnSpawn(Projectile projectile, IEntitySource source)
	{
		AttackInstanceId = 0;
		OriginItemType = 0;
		AmmoItemType = 0;
		AcquisitionTag = DamageTags.UnspecifiedAcquisition;
		ElementTag = DamageTags.Neutral;

		if (source is EntitySource_ItemUse itemUse)
		{
			InheritFromItem(itemUse.Player, itemUse.Item);
			if (source is EntitySource_ItemUse_WithAmmo withAmmo)
				AmmoItemType = withAmmo.AmmoItemIdUsed;
		}
		else if (source is EntitySource_Parent parent)
		{
			if (parent.Entity is Projectile parentProjectile)
				InheritFromProjectile(parentProjectile);
			else if (parent.Entity is Player player)
				AttackInstanceId = player.GetModPlayer<DamageCombatPlayer>().EnsureAttack();
		}
		else if (projectile.owner >= 0
			&& projectile.owner < Main.maxPlayers
			&& Main.netMode != NetmodeID.Server
			&& projectile.owner == Main.myPlayer)
		{
			AttackInstanceId = Main.player[projectile.owner].GetModPlayer<DamageCombatPlayer>().EnsureAttack();
		}
	}

	public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
	{
		binaryWriter.Write(AttackInstanceId);
		binaryWriter.Write7BitEncodedInt(OriginItemType);
		binaryWriter.Write7BitEncodedInt(AmmoItemType);
		DamageTagWire.Write(binaryWriter, AcquisitionTag);
		DamageTagWire.Write(binaryWriter, ElementTag);
	}

	public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
	{
		AttackInstanceId = binaryReader.ReadUInt64();
		OriginItemType = binaryReader.Read7BitEncodedInt();
		AmmoItemType = binaryReader.Read7BitEncodedInt();
		AcquisitionTag = DamageTagWire.SanitizeAcquisition(DamageTagWire.Read(binaryReader), DamageTags.UnspecifiedAcquisition);
		ElementTag = DamageTagWire.SanitizeElement(DamageTagWire.Read(binaryReader), DamageTags.Neutral);
	}

	public uint HitEventId(Projectile projectile, bool trueMultistrike)
	{
		if (!trueMultistrike)
			return 1;

		unchecked
		{
			uint projectilePart = ((uint)projectile.identity + 1u) & 0xffffu;
			uint hitPart = ((uint)projectile.numHits + 1u) & 0xffffu;
			return (projectilePart << 16) | (hitPart == 0 ? 1u : hitPart);
		}
	}

	public bool MatchesHitEvent(Projectile projectile, bool trueMultistrike, uint eventId)
	{
		if (!trueMultistrike)
			return eventId == 1;
		uint projectilePart = ((uint)projectile.identity + 1u) & 0xffffu;
		return eventId != 0 && (eventId >> 16) == projectilePart && (eventId & 0xffffu) != 0;
	}

	internal ulong EnsureServerAttackInstance(ServerAttackInstanceIdAllocator allocator)
	{
		if (AttackInstanceId == 0)
			AttackInstanceId = allocator.Issue();
		return AttackInstanceId;
	}

	private void InheritFromItem(Player player, Item item)
	{
		AttackInstanceId = player.GetModPlayer<DamageCombatPlayer>().EnsureAttack();
		OriginItemType = item.type;
		DamageAffinityGlobalItem affinity = item.GetGlobalItem<DamageAffinityGlobalItem>();
		AcquisitionTag = affinity.AcquisitionTag;
		ElementTag = affinity.ElementTag;
	}

	private void InheritFromProjectile(Projectile parent)
	{
		DamageLineageGlobalProjectile lineage = parent.GetGlobalProjectile<DamageLineageGlobalProjectile>();
		AttackInstanceId = lineage.AttackInstanceId;
		OriginItemType = lineage.OriginItemType;
		AmmoItemType = lineage.AmmoItemType;
		AcquisitionTag = lineage.AcquisitionTag;
		ElementTag = lineage.ElementTag;
	}
}
