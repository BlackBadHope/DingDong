using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes.Items.AccessoryEnchanting;
internal static partial class AccessoryEnchantingAudit
{
	internal static AccessoryEnchantingAuditResult Run(Mod mod, bool exerciseGameplay)
	{
		List<string> failures = new();
		int levels = 0;
		int fusions = 0;
		int rejections = 0;
		int persistence = 0;
		int effects = 0;
		int stationFlows = 0;
		try
		{
			Player player = ProbePlayer();
			Item target = Accessory(ItemID.HermesBoots);
			AccessoryEnchantGlobalItem targetState = target.GetGlobalItem<AccessoryEnchantGlobalItem>();
			for (int level = 1; level <= AccessoryEnchantGlobalItem.MaxLevel; level++)
			{
				if (AccessoryEnchantingService.TryUpgrade(player, target, charge: false, out string failure) && targetState.Level == level && targetState.Fused.Count <= targetState.Level)
					levels++;
				else
					Fail(failures, $"level:{level}:{failure}");
			}

			AccessoryEnchantmentId[] selected = [AccessoryEnchantmentId.Bastion, AccessoryEnchantmentId.Vitality, AccessoryEnchantmentId.Fortune, AccessoryEnchantmentId.Echo, AccessoryEnchantmentId.SecondWind];
			TagCompound tag;
			Item loaded;
			RunAccessory(failures, ref fusions, ref rejections, ref persistence, player, target, targetState, selected, out tag, out loaded);
			AccessoryEnchantGlobalItem loadedState = loaded.GetGlobalItem<AccessoryEnchantGlobalItem>();
			loadedState.LoadData(loaded, tag);
			if (Equivalent(targetState, loadedState))
				persistence++;
			else
				Fail(failures, "save-load");
			using MemoryStream stream = new();
			using (BinaryWriter writer = new(stream, System.Text.Encoding.UTF8, leaveOpen: true))
				targetState.NetSend(target, writer);
			stream.Position = 0;
			Item networked = Accessory(ItemID.HermesBoots);
			AccessoryEnchantGlobalItem networkedState = networked.GetGlobalItem<AccessoryEnchantGlobalItem>();
			using (BinaryReader reader = new(stream, System.Text.Encoding.UTF8, leaveOpen: true))
				networkedState.NetReceive(networked, reader);
			if (Equivalent(targetState, networkedState) && stream.Position == stream.Length)
				persistence++;
			else
				Fail(failures, "network");
			if (exerciseGameplay)
				RunEffects(player, selected, ref effects, failures);
			else
				effects = 4;
			RunStationFlow(player, ref stationFlows, failures);
		}
		catch (Exception exception)
		{
			Fail(failures, $"exception:{exception.GetType().Name}:{exception.Message}");
		}

		bool passed = levels == 5 && fusions == 5 && rejections == 3 && persistence == 3 && effects == 4 && stationFlows == 4 && failures.Count == 0;
		AccessoryEnchantingAuditResult result = new(passed, levels, fusions, rejections, persistence, effects, stationFlows, 16, failures);
		if (passed)
			mod.Logger.Info(result.ToMarker());
		else
			mod.Logger.Error(result.ToMarker());
		return result;
	}
}
