using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
internal static class DamageNetworkCodec
{
	public const byte ProtocolVersion = 2;
	public static bool IsSupportedVersion(byte version) => version == ProtocolVersion;
	public static bool AreSupportedStrikeFlags(byte flags) => (flags & ~7) == 0;
	public static void WriteIntent(BinaryWriter writer, DamageHitIntent intent)
	{
		writer.Write((byte)DamagePacketKind.HitIntent);
		writer.Write(ProtocolVersion);
		writer.Write((byte)intent.SourceKind);
		writer.Write((short)intent.TargetId);
		writer.Write7BitEncodedInt(intent.SourceType);
		writer.Write7BitEncodedInt(intent.ProjectileIdentity + 1);
		writer.Write(intent.AttackInstanceId);
		writer.Write(intent.HitEventId);
		writer.Write(intent.ClientTick);
		writer.Write(intent.PredictedMultiplier);
	}

	public static DamageHitIntent ReadIntent(BinaryReader reader)
	{
		byte version = reader.ReadByte();
		if (!IsSupportedVersion(version))
			throw new InvalidDataException($"Unsupported damage protocol version {version}.");
		DamageSourceKind sourceKind = (DamageSourceKind)reader.ReadByte();
		if (sourceKind is not (DamageSourceKind.Item or DamageSourceKind.Projectile))
			throw new InvalidDataException($"Unsupported damage source kind {(byte)sourceKind}.");
		return new DamageHitIntent(sourceKind, reader.ReadInt16(), reader.Read7BitEncodedInt(), reader.Read7BitEncodedInt() - 1, reader.ReadUInt64(), reader.ReadUInt32(), reader.ReadInt64(), reader.ReadSingle());
	}

	public static void WriteCommit(BinaryWriter writer, DamageHitCommit commit)
	{
		writer.Write((byte)DamagePacketKind.HitCommit);
		writer.Write(ProtocolVersion);
		writer.Write((byte)commit.SourceKind);
		writer.Write((short)commit.TargetId);
		writer.Write7BitEncodedInt(commit.SourceType);
		writer.Write7BitEncodedInt(commit.ProjectileIdentity + 1);
		writer.Write(commit.AttackInstanceId);
		writer.Write(commit.HitEventId);
		WriteStrikePayload(writer, commit.Payload);
	}

	public static DamageHitCommit ReadCommit(BinaryReader reader)
	{
		byte version = reader.ReadByte();
		if (!IsSupportedVersion(version))
			throw new InvalidDataException($"Unsupported damage protocol version {version}.");
		DamageSourceKind sourceKind = (DamageSourceKind)reader.ReadByte();
		if (sourceKind is not (DamageSourceKind.Item or DamageSourceKind.Projectile))
			throw new InvalidDataException($"Unsupported damage source kind {(byte)sourceKind}.");
		return new DamageHitCommit(sourceKind, reader.ReadInt16(), reader.Read7BitEncodedInt(), reader.Read7BitEncodedInt() - 1, reader.ReadUInt64(), reader.ReadUInt32(), ReadStrikePayload(reader));
	}

	public static void WriteReceipt(BinaryWriter writer, DamageHitReceipt receipt)
	{
		writer.Write((byte)DamagePacketKind.HitReceipt);
		writer.Write(ProtocolVersion);
		writer.Write(receipt.Accepted);
		writer.Write((short)receipt.TargetId);
		writer.Write(receipt.AttackInstanceId);
		writer.Write(receipt.ServerMultiplier);
		writer.Write((byte)receipt.Reason);
	}

	public static DamageHitReceipt ReadReceipt(BinaryReader reader)
	{
		byte version = reader.ReadByte();
		if (!IsSupportedVersion(version))
			throw new InvalidDataException($"Unsupported damage protocol version {version}.");
		return new DamageHitReceipt(reader.ReadBoolean(), reader.ReadInt16(), reader.ReadUInt64(), reader.ReadSingle(), (DamageReceiptReason)reader.ReadByte());
	}

	public static void WriteStrikePayload(BinaryWriter writer, DamageStrikePayload payload)
	{
		writer.Write7BitEncodedInt(payload.Damage);
		writer.Write7BitEncodedInt(payload.SourceDamage);
		writer.Write7BitEncodedInt(payload.DamageType);
		writer.Write((sbyte)payload.HitDirection);
		writer.Write(payload.Knockback);
		byte flags = 0;
		if (payload.Crit)
			flags |= 1;
		if (payload.InstantKill)
			flags |= 2;
		if (payload.HideCombatText)
			flags |= 4;
		writer.Write(flags);
	}

	public static DamageStrikePayload ReadStrikePayload(BinaryReader reader)
	{
		DamageStrikePayload payload = new(reader.Read7BitEncodedInt(), reader.Read7BitEncodedInt(), reader.Read7BitEncodedInt(), reader.ReadSByte(), reader.ReadSingle(), false, false, false);
		byte flags = reader.ReadByte();
		if (!AreSupportedStrikeFlags(flags))
			throw new InvalidDataException($"Unsupported StrikeNPC flags 0x{flags:X2}.");
		return payload with
		{
			Crit = (flags & 1) != 0,
			InstantKill = (flags & 2) != 0,
			HideCombatText = (flags & 4) != 0
		};
	}
}
