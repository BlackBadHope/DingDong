using System;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes.Items.MobRelics;

internal static partial class MobRelicAudit
{
	private static void AuditCloneAndStack(MobRelicAuditContext context, GeneratedMobRelicItem relic, Item original)
	{
		try
		{
			Item cloned = original.Clone();
			if (SameIdentity(original, cloned)) context.ClonePassed++; else Failure(context, $"clone:{relic.Name}");
			if (ItemLoader.CanStack(original, cloned)) context.StackPassed++; else Failure(context, $"stack:{relic.Name}");
			if (ItemLoader.CanStackInWorld(original, cloned)) context.WorldStackPassed++; else Failure(context, $"world-stack:{relic.Name}");
		}
		catch (Exception exception)
		{
			Failure(context, $"clone-stack:{relic.Name}:{exception.GetType().Name}");
		}
	}

	private static void AuditPersistence(MobRelicAuditContext context, GeneratedMobRelicItem relic, Item original)
	{
		try
		{
			TagCompound tag = ItemIO.Save(original);
			Item loaded = ItemIO.Load(tag);
			if (SameIdentity(original, loaded)) context.SaveLoadPassed++; else Failure(context, $"save:{relic.Name}");
		}
		catch (Exception exception)
		{
			Failure(context, $"save:{relic.Name}:{exception.GetType().Name}");
		}

		try
		{
			using MemoryStream stream = new();
			using (BinaryWriter writer = new(stream, System.Text.Encoding.UTF8, leaveOpen: true))
				ItemIO.Send(original, writer, true, false);
			stream.Position = 0;
			using BinaryReader reader = new(stream, System.Text.Encoding.UTF8, leaveOpen: true);
			Item networked = ItemIO.Receive(reader, true, false);
			if (SameIdentity(original, networked)) context.NetPassed++; else Failure(context, $"net:{relic.Name}");
		}
		catch (Exception exception)
		{
			Failure(context, $"net:{relic.Name}:{exception.GetType().Name}");
		}
	}
}
