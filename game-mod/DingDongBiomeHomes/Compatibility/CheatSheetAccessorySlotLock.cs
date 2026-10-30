using System;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Compatibility;

// Owner, 30.10: every player has six extra accessory slots by default, and Cheat Sheet's bottom-bar
// button can no longer add or remove them. Cheat Sheet keeps the items of disabled slots in its own
// array and equips only the first numberExtraAccessoriesEnabled of them, so the count is simply held
// at six on every side each tick; a click on the button is undone on the next tick.
public sealed class CheatSheetAccessorySlotLock : ModPlayer
{
	internal const int LockedSlots = 6;
	private static ModPlayer template;
	private static FieldInfo countField;
	private static int target;

	public override bool IsLoadingEnabled(Mod mod) => ModLoader.HasMod("CheatSheet");

	public override void SetStaticDefaults()
	{
		try
		{
			if (!ModLoader.TryGetMod("CheatSheet", out Mod cheatSheet)) return;
			Type type = cheatSheet.Code.GetTypes().FirstOrDefault(t => t.Name == "CheatSheetPlayer" && typeof(ModPlayer).IsAssignableFrom(t));
			if (type == null) return;
			const BindingFlags any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
			countField = type.GetField("numberExtraAccessoriesEnabled", any);
			if (countField == null || countField.FieldType != typeof(int)) { countField = null; return; }
			FieldInfo maxField = type.GetField("MaxExtraAccessories", any);
			object max = maxField == null ? null : maxField.IsLiteral ? maxField.GetRawConstantValue() : maxField.GetValue(null);
			target = max is int limit ? Math.Min(LockedSlots, limit) : LockedSlots;
			template = cheatSheet.GetContent<ModPlayer>().FirstOrDefault(p => p.GetType() == type);
			Mod.Logger.Info($"DINGDONG_CHEATSHEET_ACCESSORIES|state=LOCKED|slots={target}|max={max ?? "unknown"}");
		}
		catch (Exception error)
		{
			countField = null;
			Mod.Logger.Warn("DINGDONG_CHEATSHEET_ACCESSORIES|state=DISABLED|reason=" + error.Message);
		}
	}

	public override void Unload() { template = null; countField = null; }

	public override void OnEnterWorld() => Hold();
	public override void PostUpdate() => Hold();

	private void Hold()
	{
		if (countField == null || template == null) return;
		ModPlayer state = Player.GetModPlayer(template);
		if (state != null && (int)countField.GetValue(state) != target) countField.SetValue(state, target);
	}
}
