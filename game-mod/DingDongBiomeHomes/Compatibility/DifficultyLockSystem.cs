using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using log4net;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.GameContent.NetModules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Net;

namespace DingDongBiomeHomes.Compatibility;

// V03 28.10 (owner decision): Fargo's Eternity Mode in its top form (Masochist = Eternity on an
// effective Master world) and Calamity's Death Mode (with Revengeance) are always on and cannot be
// switched off. Floor = Death + Masochist; a harder choice (Calamity Malice on FTW Master) stays allowed.
// Soft dependencies: reflection and our own detours only, never their files. Server/SP set the flags
// at world creation and load and re-assert them before every world update (before Calamity's and
// Fargo's PostUpdateWorld can react); WorldData sync carries them to clients. Clients refuse the
// toggles locally (Mutant's Gift, Fargowiltas mode toggle, Calamity mode indicator, Thorium World Engine).
public sealed class DifficultyLockSystem : ModSystem
{
	internal const string LocKey = "Mods.DingDongBiomeHomes.DifficultyLock";
	private const BindingFlags StaticAny = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

	private static readonly List<IDisposable> hooks = new();
	internal static readonly List<string> InstalledHooks = new();
	internal static readonly List<string> ContractFailures = new();
	internal static int RepairsAtWorldGen, RepairsAtLoad, RepairsAtTick;
	private static ILog log;
	private static Flag fargoShould, fargoEternity, fargoMaso, fargoCanPlayMaso, calRevenge, calDeath;
	private static Type calDeathType, calMaliceType;
	private static FieldInfo sliderValue;
	private static MethodInfo sliderUpdate;
	private static LocalizedText calamityLockText;
	private static int journeyTimer, tickLogCooldown;
	private static uint lastRefusal;

	internal static bool FargoBound => fargoShould != null;
	internal static bool CalamityBound => calDeath != null;
	internal static bool FargoEternity => fargoEternity?.Get() == true && fargoShould.Get();
	internal static bool FargoMasochist => fargoMaso?.Get() == true;
	internal static bool CalamityDeath => calDeath?.Get() == true && calRevenge.Get();

	// Death (non-FTW) and Fargo Masochist need Master; on For the Worthy, Expert already counts as Master.
	internal static int RequiredGameMode() => Main.getGoodWorld ? GameModeID.Expert : GameModeID.Master;

	public override void Load()
	{
		log = Mod.Logger;
		if (ModLoader.TryGetMod("FargowiltasSouls", out Mod fargo)) Guard("FargowiltasSouls", () => InstallFargo(fargo));
		if (ModLoader.TryGetMod("CalamityMod", out Mod calamity)) Guard("CalamityMod", () => InstallCalamity(calamity));
		if (ModLoader.TryGetMod("ThoriumMod", out Mod thorium)) Guard("ThoriumMod", () => InstallThorium(thorium));
		Guard("Journey", BindJourneySlider);
		log.Info($"DINGDONG_DIFFICULTY_LOCK|stage=install|fargo={FargoBound}|calamity={CalamityBound}|hooks={string.Join(",", InstalledHooks)}|contractFailures={ContractFailures.Count}");
	}

	public override void Unload()
	{
		foreach (IDisposable hook in hooks) hook.Dispose();
		hooks.Clear(); InstalledHooks.Clear(); ContractFailures.Clear();
		fargoShould = fargoEternity = fargoMaso = fargoCanPlayMaso = calRevenge = calDeath = null;
		calDeathType = calMaliceType = null; sliderValue = null; sliderUpdate = null; calamityLockText = null; log = null;
	}

	public override void PreWorldGen() => RepairsAtWorldGen = 0;
	public override void PostWorldGen() { RepairsAtWorldGen += Enforce(full: true); Report("worldgen", RepairsAtWorldGen); }
	public override void OnWorldLoad() { RepairsAtLoad = 0; RepairsAtTick = 0; journeyTimer = 0; tickLogCooldown = 0; }

	public override void PostWorldLoad()
	{
		if (Main.netMode == NetmodeID.MultiplayerClient) return;
		RepairsAtLoad += Enforce(full: true);
		Report("load", RepairsAtLoad);
	}

	// Runs on server/SP only (vanilla UpdateWorld) and before every mod's PostUpdateWorld.
	public override void PreUpdateWorld()
	{
		if (tickLogCooldown > 0) tickLogCooldown--;
		int repaired = Enforce(full: false);
		if (repaired == 0) return;
		RepairsAtTick += repaired;
		if (tickLogCooldown == 0) { tickLogCooldown = 600; Report("tick", repaired); }
		if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.WorldData);
	}

	// Returns how many flags had to be restored. No allocations on the steady path.
	internal static int Enforce(bool full)
	{
		int repaired = 0;
		if (Main.GameMode == GameModeID.Creative) repaired += EnforceJourneySlider(full);
		else if (Main.GameMode < RequiredGameMode()) { Main.GameMode = RequiredGameMode(); repaired++; }
		if (fargoShould != null)
		{
			repaired += fargoShould.Ensure() + fargoCanPlayMaso.Ensure();
			// Fargo itself turns Eternity/Masochist on once no boss is alive; at creation/load set them at once.
			if (full) repaired += fargoEternity.Ensure() + fargoMaso.Ensure();
		}
		if (calDeath != null) repaired += calRevenge.Ensure() + calDeath.Ensure();
		return repaired;
	}

	private static void Report(string stage, int repaired) =>
		log?.Info($"DINGDONG_DIFFICULTY_LOCK|stage={stage}|repaired={repaired}|gameMode={Main.GameMode}|ftw={Main.getGoodWorld}|masterMode={Main.masterMode}" +
			$"|eternity={(FargoBound ? FargoEternity.ToString() : "absent")}|masochist={(FargoBound ? FargoMasochist.ToString() : "absent")}" +
			$"|revengeanceDeath={(CalamityBound ? CalamityDeath.ToString() : "absent")}|contractFailures={ContractFailures.Count}");

	private static void InstallFargo(Mod fargo)
	{
		Type world = fargo.Code.GetType("FargowiltasSouls.Core.Systems.WorldSavingSystem", true);
		Flag should = Flag.Bind(world, "ShouldBeEternityMode"), eternity = Flag.Bind(world, "EternityMode"),
			maso = Flag.Bind(world, "MasochistModeReal"), canPlay = Flag.Bind(world, "CanPlayMaso");
		fargoShould = should; fargoEternity = eternity; fargoMaso = maso; fargoCanPlayMaso = canPlay;
		// Standard and plain Eternity in the difficulty menu would lower the mode; Masochist stays allowed.
		foreach (string option in new[] { "VanillaDifficultyOption.DisableEternity", "EternityDifficultyOption.EnableEternity" })
		{
			string[] part = option.Split('.');
			Type type = fargo.Code.GetType("FargowiltasSouls.Content.UI.Elements." + part[0], true);
			MethodInfo method = type.GetMethod(part[1], StaticAny, null, Type.EmptyTypes, null);
			if (method?.ReturnType != typeof(void)) throw new MissingMethodException(type.FullName, part[1]);
			AddHook("Fargo." + part[1], () => new Hook(method, (VoidHook)RefuseVoid));
		}
	}

	private static void InstallCalamity(Mod calamity)
	{
		Type world = calamity.Code.GetType("CalamityMod.World.CalamityWorld", true);
		Flag revenge = Flag.Bind(world, "revenge"), death = Flag.Bind(world, "death");
		Type mode = calamity.Code.GetType("CalamityMod.Systems.DifficultyMode", true);
		Type death2 = calamity.Code.GetType("CalamityMod.Systems.DeathDifficulty", true);
		Type malice = calamity.Code.GetType("CalamityMod.Systems.MaliceDifficulty", true);
		Type ui = calamity.Code.GetType("CalamityMod.UI.ModeIndicator.ModeIndicatorUI", true);
		MethodInfo switchTo = ui.GetMethod("SwitchToDifficulty", StaticAny, null, new[] { mode, typeof(bool) }, null)
			?? throw new MissingMethodException(ui.FullName, "SwitchToDifficulty");
		MethodInfo lockStatus = ui.GetMethod("GetLockStatus", StaticAny, null,
			new[] { typeof(LocalizedText).MakeByRefType(), typeof(bool).MakeByRefType() }, null)
			?? throw new MissingMethodException(ui.FullName, "GetLockStatus");
		calRevenge = revenge; calDeath = death; calDeathType = death2; calMaliceType = malice;
		AddHook("Calamity.SwitchToDifficulty", () => new ILHook(switchTo, GuardCalamitySwitch));
		AddHook("Calamity.GetLockStatus", () => new Hook(lockStatus, (LockHook)CalamityLockStatus));
	}

	private static void InstallThorium(Mod thorium)
	{
		Type tile = thorium.Code.GetType("ThoriumMod.Tiles.WorldEngineTile", true);
		MethodInfo set = tile.GetMethod("SetDifficulty", StaticAny, null, new[] { typeof(int), typeof(bool) }, null)
			?? throw new MissingMethodException(tile.FullName, "SetDifficulty");
		MethodInfo toggle = tile.GetMethod("ToggleDifficulty", StaticAny, null, new[] { typeof(int), typeof(int) }, null)
			?? throw new MissingMethodException(tile.FullName, "ToggleDifficulty");
		AddHook("Thorium.SetDifficulty", () => new Hook(set, (SetDifficultyHook)GuardThoriumSet));
		AddHook("Thorium.ToggleDifficulty", () => new Hook(toggle, (ToggleHook)GuardThoriumToggle));
	}

	private static void BindJourneySlider()
	{
		sliderValue = typeof(CreativePowers.ASharedSliderPower).GetField("_sliderCurrentValueCache", BindingFlags.NonPublic | BindingFlags.Instance)
			?? throw new MissingFieldException(nameof(CreativePowers.ASharedSliderPower), "_sliderCurrentValueCache");
		sliderUpdate = typeof(CreativePowers.DifficultySliderPower).GetMethod("UpdateInfoFromSliderValueCache", BindingFlags.NonPublic | BindingFlags.Instance, Type.EmptyTypes)
			?? throw new MissingMethodException(nameof(CreativePowers.DifficultySliderPower), "UpdateInfoFromSliderValueCache");
	}

	// Journey worlds keep their game mode; the shared difficulty slider is held at Master (1.0) the way
	// vanilla applies a slider packet on the server. Checked twice a second (reflection boxes one float).
	private static int EnforceJourneySlider(bool full)
	{
		if (sliderValue == null || (!full && ++journeyTimer < 30)) return 0;
		journeyTimer = 0;
		CreativePowers.DifficultySliderPower power = CreativePowerManager.Instance.GetPower<CreativePowers.DifficultySliderPower>();
		if (power == null || (float)sliderValue.GetValue(power) >= 1f) return 0;
		sliderValue.SetValue(power, 1f);
		sliderUpdate.Invoke(power, null);
		if (Main.netMode == NetmodeID.Server)
		{
			NetPacket packet = NetCreativePowersModule.PreparePacket(power.PowerId, 4);
			packet.Writer.Write(1f);
			NetManager.Instance.Broadcast(packet);
		}
		return 1;
	}

	private static void Guard(string scope, Action install)
	{
		try { install(); }
		catch (Exception ex) when (ex is not OutOfMemoryException)
		{
			ContractFailures.Add(scope + ": " + ex.GetType().Name + ": " + ex.Message);
			log?.Error($"DINGDONG_DIFFICULTY_LOCK|verdict=CONTRACT_CHANGED|scope={scope}|error={ex.GetType().Name}: {ex.Message}");
		}
	}

	private static void AddHook(string name, Func<IDisposable> create)
	{
		hooks.Add(create());
		InstalledHooks.Add(name);
	}

	internal static bool IsLockedToggleItem(Item item) => item?.ModItem is ModItem m &&
		((m.Name == "Masochist" && m.Mod.Name == "FargowiltasSouls") || (m.Name == "ModeToggle" && m.Mod.Name == "Fargowiltas"));

	// Local chat note, at most every two seconds (CanUseItem runs every frame while the mouse is held).
	internal static void NotifyRefused()
	{
		if (Main.dedServ) return;
		uint now = Main.GameUpdateCount;
		if (lastRefusal != 0 && now - lastRefusal < 120) return;
		lastRefusal = now == 0 ? 1u : now;
		Main.NewText(Language.GetTextValue(LocKey + ".Refused"), new Color(255, 120, 120));
	}

	private delegate void VoidOrig();
	private delegate void VoidHook(VoidOrig orig);
	private static void RefuseVoid(VoidOrig orig) => NotifyRefused();

	private delegate void LockOrig(out LocalizedText text, out bool locked);
	private delegate void LockHook(LockOrig orig, out LocalizedText text, out bool locked);
	// The mode indicator shows its own lock icon and never opens the selection bar.
	private static void CalamityLockStatus(LockOrig orig, out LocalizedText text, out bool locked)
	{
		orig(out text, out locked);
		locked = true;
		text = calamityLockText ??= Language.GetText(LocKey + ".CalamityLocked");
	}

	private static void GuardCalamitySwitch(ILContext il)
	{
		var c = new ILCursor(il);
		Instruction original = c.Next;
		c.Emit(OpCodes.Ldarg_0);
		c.EmitDelegate<Func<object, bool>>(AllowCalamitySwitch);
		c.Emit(OpCodes.Brtrue, original);
		c.Emit(OpCodes.Ret);
	}

	// Only Death and Malice keep CalamityWorld.revenge and death set; every other target would clear them.
	private static bool AllowCalamitySwitch(object mode)
	{
		if (mode == null || calDeathType == null) return true;
		Type type = mode.GetType();
		if (calDeathType.IsAssignableFrom(type) || calMaliceType.IsAssignableFrom(type)) return true;
		NotifyRefused();
		return false;
	}

	private delegate void SetDifficultyOrig(int difficulty, bool request);
	private delegate void SetDifficultyHook(SetDifficultyOrig orig, int difficulty, bool request);
	private static void GuardThoriumSet(SetDifficultyOrig orig, int difficulty, bool request)
	{
		if (Main.GameMode == GameModeID.Creative || difficulty < RequiredGameMode()) { NotifyRefused(); return; }
		orig(difficulty, request);
	}

	private delegate void ToggleOrig(int i, int j);
	private delegate void ToggleHook(ToggleOrig orig, int i, int j);
	// Refused before Thorium consumes the five Fallen Stars; target as in WorldEngineTile.ToggleDifficulty.
	private static void GuardThoriumToggle(ToggleOrig orig, int i, int j)
	{
		int target = WorldGen.InWorld(i, j) ? (Main.tile[i, j].TileFrameX / 18) switch { 1 => 1, 0 => 0, _ => 2 } : 0;
		if (Main.GameMode != GameModeID.Creative && target < RequiredGameMode()) { NotifyRefused(); return; }
		orig(i, j);
	}

	// Compiled static bool accessor (field or property): no boxing on the per-tick path.
	private sealed class Flag
	{
		internal readonly Func<bool> Get;
		private readonly Action<bool> set;
		private Flag(Func<bool> get, Action<bool> set) { Get = get; this.set = set; }

		internal int Ensure()
		{
			if (Get()) return 0;
			set(true);
			return 1;
		}

		internal static Flag Bind(Type type, string name)
		{
			MemberExpression member = type.GetField(name, StaticAny) is FieldInfo field && field.FieldType == typeof(bool) && !field.IsInitOnly
				? Expression.Field(null, field)
				: type.GetProperty(name, StaticAny) is PropertyInfo property && property.PropertyType == typeof(bool) && property.CanRead && property.CanWrite
					? Expression.Property(null, property)
					: throw new MissingMemberException(type.FullName, name);
			ParameterExpression value = Expression.Parameter(typeof(bool), "value");
			return new Flag(Expression.Lambda<Func<bool>>(member).Compile(),
				Expression.Lambda<Action<bool>>(Expression.Assign(member, value), value).Compile());
		}
	}
}

public sealed class DifficultyLockGlobalItem : GlobalItem
{
	public override bool AppliesToEntity(Item entity, bool lateInstantiation) => DifficultyLockSystem.IsLockedToggleItem(entity);

	public override bool CanUseItem(Item item, Player player)
	{
		if (player.whoAmI == Main.myPlayer) DifficultyLockSystem.NotifyRefused();
		return false;
	}

	public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) =>
		tooltips.Add(new TooltipLine(Mod, "DingDongDifficultyLock", Language.GetTextValue(DifficultyLockSystem.LocKey + ".ItemLocked"))
		{ OverrideColor = new Color(255, 120, 120) });
}
