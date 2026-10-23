using System;
using System.IO;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;

public sealed partial class SettlementWorldState
{
	internal static void RunWireProbe(Mod mod)
	{
		var state = ModContent.GetInstance<SettlementWorldState>();
		TagCompound original = new(); state.SaveWorldData(original);
		int checks = 0;
		void Check(bool ok, string id) { if (!ok) throw new InvalidOperationException(id); checks++; mod.Logger.Info("DINGDONG_RESIDENCE_WIRE_CASE|PASS|" + id); }
		try
		{
			Check(HasCompleteResidenceRegistry && LoadedFromSave, "WIRE-01 legacy tagged world loaded all 61 residences");
			using MemoryStream wire = new();
			using (BinaryWriter writer = new(wire, System.Text.Encoding.UTF8, true)) state.NetSend(writer);
			wire.Position = 0;
			using (BinaryReader reader = new(wire, System.Text.Encoding.UTF8, true)) state.NetReceive(reader);
			Check(HasCompleteResidenceRegistry && wire.Position == wire.Length, "WIRE-02 full V4 report consumed all coordinates");
			using MemoryStream old = new();
			using (BinaryWriter writer = new(old, System.Text.Encoding.UTF8, true)) { writer.Write(true); writer.Write(3); }
			old.Position = 0;
			bool rejected = false;
			try { using BinaryReader reader = new(old); state.NetReceive(reader); } catch (InvalidDataException) { rejected = true; }
			Check(rejected && !HasReport, "WIRE-03 old V3 layout rejected before residence payload");
			byte[] bytes = wire.ToArray();
			using MemoryStream truncated = new(bytes, 0, bytes.Length - 1, false);
			rejected = false;
			try { using BinaryReader reader = new(truncated); state.NetReceive(reader); } catch (EndOfStreamException) { rejected = true; }
			Check(rejected && !HasReport, "WIRE-04 truncated V4 rejected");
			var config = ModContent.GetInstance<DingDongServerConfig>();
			Check(config.EnableNpcQuestsAndEscort && config.EnableFiveTileBaseJump && config.EnableNightPressure && config.RequiredResidentQuests == 3,
				"WIRE-05 actual Live doctrine defaults enabled without probe overrides");
		}
		finally { state.LoadWorldData(original); }
		mod.Logger.Info($"DINGDONG_RESIDENCE_WIRE|verdict=PASS|checks={checks}|reportVersion={CurrentReportVersion}|legacySave=True|socketMultiplayer=False|worldSaved=False");
	}
}
