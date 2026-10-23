﻿using System;
using System.Collections.Generic;
using System.Globalization;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Net;
using DingDongBiomeHomes.Worldgen;

namespace DingDongBiomeHomes;

// 0.28.10 network hardening (audit R1-R7, 2026-10-22).
internal static class NetworkHardeningLimits
{
	internal const int MaxSettlementDistricts = 1024;
	internal const int MaxSettlementResidences = 4096;
}

/// <summary>R3/R4: per-sender reject counter, rate-limited logging, kick on flood.</summary>
internal static class PacketRejectGuard
{
	internal const double WindowSeconds = 10;
	internal const int LoggedPerWindow = 3;
	internal const int KickThreshold = 24;
	private const int Slots = 257;
	private static readonly int[] Count = new int[Slots];
	private static readonly double[] WindowStart = new double[Slots];

	internal static int CountFor(int who) => Count[Slot(who)];
	private static int Slot(int who) => who >= 0 && who < Main.maxPlayers ? who : Slots - 1;

	internal static void Reject(string channel, int who, byte kind, string reason)
	{
		int slot = Slot(who);
		double now = System.Diagnostics.Stopwatch.GetTimestamp() / (double)System.Diagnostics.Stopwatch.Frequency;
		if (now - WindowStart[slot] >= WindowSeconds || Count[slot] == 0)
		{
			if (Count[slot] > LoggedPerWindow)
				DingDongBiomeHomes.Instance?.Logger.Warn($"DINGDONG_PACKET_REJECT_SUPPRESSED|sender={who}|suppressed={Count[slot] - LoggedPerWindow}");
			WindowStart[slot] = now;
			Count[slot] = 0;
		}
		int count = ++Count[slot];
		if (count <= LoggedPerWindow)
			DingDongBiomeHomes.Instance?.Logger.Warn($"DINGDONG_{channel}_PACKET_REJECT|sender={who}|kind={kind}|reason={reason}|count={count}");
		if (count >= KickThreshold && Main.netMode == NetmodeID.Server && slot != Slots - 1)
		{
			DingDongBiomeHomes.Instance?.Logger.Warn($"DINGDONG_PACKET_REJECT_KICK|sender={who}|count={count}|window={WindowSeconds}s");
			Clear(who);
			NetMessage.BootPlayer(who, NetworkText.FromLiteral("DingDong: too many invalid network packets."));
		}
	}

	internal static void Clear(int who)
	{
		int slot = Slot(who);
		Count[slot] = 0;
		WindowStart[slot] = 0;
	}

	internal static void ClearAll()
	{
		Array.Clear(Count);
		Array.Clear(WindowStart);
	}
}

/// <summary>R1: bind client-declared quest GUID to the server-observed platform identity (trust on first use).</summary>
internal sealed class QuestIdentityBindingSystem : ModSystem
{
	private const string TagKey = "QuestIdentityBindingsV1";
	private static readonly Dictionary<string, string> Bindings = new(StringComparer.Ordinal);

	internal static string PlatformKey(int who)
	{
		try
		{
			RemoteAddress address = Netplay.Clients[who]?.Socket?.GetRemoteAddress();
			if (address != null && address.Type == AddressType.Steam)
				return "steam:" + address.GetIdentifier();
		}
		catch (Exception)
		{
		}
		return "name:" + (Main.player[who]?.name ?? string.Empty);
	}

	internal static bool TryBind(int who, string persistentId, out string failure) =>
		TryBindKey(persistentId, Main.netMode == NetmodeID.Server ? PlatformKey(who) : "local", out failure);

	internal static bool TryBindKey(string persistentId, string platformKey, out string failure)
	{
		failure = null;
		if (Bindings.TryGetValue(persistentId, out string owner))
		{
			if (string.Equals(owner, platformKey, StringComparison.Ordinal))
				return true;
			failure = "Quest identity is bound to another account.";
			return false;
		}
		// Existing progress keyed by this GUID migrates to the first account that claims it after 0.28.10.
		Bindings[persistentId] = platformKey;
		return true;
	}

	internal static void ResetForTests() => Bindings.Clear();
	public override void ClearWorld() => Bindings.Clear();
	public override void OnWorldUnload() { Bindings.Clear(); PacketRejectGuard.ClearAll(); }

	public override void SaveWorldData(TagCompound tag)
	{
		if (Bindings.Count == 0)
			return;
		List<string> rows = new(Bindings.Count);
		foreach (KeyValuePair<string, string> pair in Bindings)
			rows.Add(pair.Key + "|" + pair.Value);
		tag[TagKey] = rows;
	}

	public override void LoadWorldData(TagCompound tag)
	{
		Bindings.Clear();
		foreach (string row in tag.GetList<string>(TagKey))
		{
			int split = row.IndexOf('|');
			if (split == 32 && Guid.TryParseExact(row[..32], "N", out _))
				Bindings[row[..32]] = row[(split + 1)..];
		}
	}
}

/// <summary>R2: imported station items must be single, known accessories with an accessory-compatible prefix.</summary>
internal static class AccessoryImportValidation
{
	internal static bool Plausible(Item item)
	{
		if (item == null || item.IsAir)
			return true;
		if (!item.accessory || item.type <= ItemID.None || item.type >= ItemLoader.ItemCount || item.stack != 1)
			return false;
		int prefix = item.prefix;
		if (prefix == 0)
			return true;
		if (prefix < 0 || prefix >= PrefixLoader.PrefixCount)
			return false;
		if (prefix < PrefixID.Count)
			return prefix >= PrefixID.Hard && prefix <= PrefixID.Violent;
		ModPrefix modPrefix = PrefixLoader.GetPrefix(prefix);
		return modPrefix != null && modPrefix.Category == PrefixCategory.Accessory;
	}
}

/// <summary>R5: bound committed strikes to the source weapon's plausible envelope.</summary>
internal static class DamagePlausibility
{
	internal const float MaxKnockback = 40f;
	internal const int UnknownSourceCap = 250_000;

	internal static int DamageCap(DamageSourceKind kind, int sourceType)
	{
		int baseDamage = 0;
		if (kind == DamageSourceKind.Item && sourceType > ItemID.None && sourceType < ItemLoader.ItemCount && ContentSamples.ItemsByType.TryGetValue(sourceType, out Item sample))
			baseDamage = sample.damage;
		return baseDamage > 0 ? Math.Max(1_000, baseDamage * 100) : UnknownSourceCap;
	}

	internal static bool Plausible(DamageHitCommit commit) =>
		PlausiblePayload(commit.Payload, DamageCap(commit.SourceKind, commit.SourceType));

	internal static bool PlausiblePayload(DamageStrikePayload payload, int cap) =>
		payload.Damage <= cap && payload.SourceDamage <= cap && payload.HitDirection is >= -1 and <= 1 &&
		float.IsFinite(payload.Knockback) && payload.Knockback >= 0f && payload.Knockback <= MaxKnockback;
}

/// <summary>R7/R3: drop per-connection streaming and reject state when a player leaves.</summary>
internal sealed class NetworkHardeningPlayer : ModPlayer
{
	public override void PlayerDisconnect()
	{
		if (Main.netMode != NetmodeID.Server)
			return;
		PacketRejectGuard.Clear(Player.whoAmI);
		ContinentalSectionStreamingNetwork.ResetPeer(Player.whoAmI);
	}
}
