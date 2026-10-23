using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Compatibility;

public sealed class PackControls : ModSystem
{
 private ILHook loadHook;
 private static readonly Dictionary<string, string> Rules = new() {
  ["CalamityMod/DashDoubleTapOverride"] = "F", ["CalamityMod/GodSlayerDash"] = "F", ["Fargowiltas/Dash"] = "F", ["Fargowiltas/Stat"] = "P", ["FargowiltasSouls/EffectToggle"] = "OemPeriod",
  ["CalamityMod/ArmorSetBonus"] = "Y", ["Fargowiltas/SetBonus"] = "Y", ["ThoriumMod/ArmorAbility"] = "Y",
  ["ThoriumMod/AccessoryAbility"] = "G", ["ThoriumMod/ThrowingTechnique"] = "U", ["ThoriumMod/Encase"] = "T",
  ["CalamityMod/RageMode"] = "V", ["CalamityMod/AdrenalineMode"] = "X", ["CalamityMod/NormalityRelocator"] = "Z",
  ["CalamityMod/GravitySwapOverride"] = "Up", ["CalamityMod/HeldItem"] = "",
  ["FargowiltasSouls/ActiveSkill1"] = "NumPad8", ["FargowiltasSouls/ActiveSkill2"] = "NumPad9", ["FargowiltasSouls/ActiveSkill3"] = "NumPad0",
  ["DingDongBiomeHomes/OpenDamageGuide"] = "F1", ["DingDongBiomeHomes/OpenQuestJournal"] = "F2",
  ["BossChecklist/ToggleLog"] = "F3", ["BossChecklist/ToggleChecklist"] = "F4",
  ["PackBuilder/OpenBuilderInterface"] = "F6", ["PlayerSwapper/ToggleMenu"] = "F7",
  ["PlayerSwapper/NextPlayer"] = "", ["PlayerSwapper/PreviousPlayer"] = "",
  ["InfiniteMunitions/RotateAmmo"] = "Mouse4",
  ["ImproveGame/MasterControl"] = "F8", ["ImproveGame/AutoTrashKeybind"] = "", ["ImproveGame/HugeInventory"] = "I",
  ["ImproveGame/BuffTracker"] = "OemQuotes", ["ImproveGame/OpenBagGUI"] = "OemOpenBrackets", ["ImproveGame/QuickShimmerGUI"] = "OemPipe",
  ["ImproveGame/GrabBagLoot"] = "", ["ImproveGame/HotbarSwitch"] = "", ["ImproveGame/DiscordRodKeybind"] = "Z",
  ["ImproveGame/ItemInteract"] = "O", ["ImproveGame/CopyItemName"] = "C",
  ["MagicStorage/PrintItemData"] = "", ["WMITF/Technical Names"] = "OemComma",
  ["miningcracks_take_on_luiafk/Luiafk Recall"] = "Home", ["miningcracks_take_on_luiafk/Luiafk Recall Back"] = "End",
  ["miningcracks_take_on_luiafk/Luiafk Buffs"] = "L", ["miningcracks_take_on_luiafk/Luiafk Buff Settings"] = "OemSemicolon"
 };
 private static readonly Dictionary<string, (ModKeybind Key, string Old, string New)> changes = new();
 private static readonly Dictionary<string, (string Old, string New)> vanilla = new() {
  ["Loadout1"] = ("F1", "F9"), ["Loadout2"] = ("F2", "F10"), ["Loadout3"] = ("F3", "F11"),
  ["ToggleCameraMode"] = ("F4", "")
 };
 private static bool ready;
 public override void Load() {
  if (Main.dedServ) return;
  loadHook = new ILHook(typeof(PlayerInput).GetMethod("Load", BindingFlags.Public | BindingFlags.Static), il => {
   foreach (var ret in il.Body.Instructions.Where(i => i.OpCode == OpCodes.Ret).ToArray()) {
    var c = new ILCursor(il); c.Goto(ret); c.EmitDelegate<Action>(MigrateProfiles);
   }
  });
 }
 public override void PostSetupContent() {
  if (Main.dedServ) return;
  FieldInfo field = typeof(ModKeybind).GetField("<DefaultBinding>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
   ?? throw new InvalidOperationException("Keybind default contract changed.");
  foreach (ModKeybind key in AllKeys()) {
   string wanted;
   if (!Rules.TryGetValue(KeyMod(key).Name + "/" + KeyName(key), out wanted)) continue;
   Set(key, wanted, field);
  }
  // SOTS registers translated display text as its internal key name. Bind by stable provider field.
  if (ModLoader.TryGetMod("SOTS", out Mod sots)) {
   Type type = sots.Code.GetType("SOTS.SOTS", true);
   foreach (var pair in new[] { ("BlinkHotKey", "Z"), ("ArmorSetHotKey", "Y"), ("MachinaBoosterHotKey", "F") })
    if (type.GetField(pair.Item1, BindingFlags.Public | BindingFlags.Static)?.GetValue(null) is ModKeybind key) Set(key, pair.Item2, field);
  }
  ready = true; MigrateProfiles();
  string path = Environment.GetEnvironmentVariable("DINGDONG_KEYBINDS_PROBE_OUTPUT");
  if (!string.IsNullOrEmpty(path)) File.WriteAllText(path, JsonSerializer.Serialize(AllKeys().Select(k => new {
   mod = KeyMod(k).Name, name = KeyName(k), fullName = KeyFullName(k), defaultBinding = KeyDefault(k), changed = changes.ContainsKey(KeyFullName(k)),
   previous = changes.TryGetValue(KeyFullName(k), out var change) ? change.Old : KeyDefault(k)
  }).ToArray()));
 }
 private static IEnumerable<ModKeybind> AllKeys() => (IEnumerable<ModKeybind>)typeof(KeybindLoader).GetProperty("Keybinds", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
 private static object KeyProperty(ModKeybind key, string name) => typeof(ModKeybind).GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(key);
 private static string KeyFullName(ModKeybind key) => (string)KeyProperty(key, "FullName");
 private static string KeyName(ModKeybind key) => (string)KeyProperty(key, "Name");
 private static string KeyDefault(ModKeybind key) => (string)KeyProperty(key, "DefaultBinding");
 private static Mod KeyMod(ModKeybind key) => (Mod)KeyProperty(key, "Mod");
 private static void Set(ModKeybind key, string wanted, FieldInfo field) {
  changes[KeyFullName(key)] = (key, KeyDefault(key), wanted); field.SetValue(key, wanted);
 }
 internal static bool IsNativeDefault(List<string> assigned, string native) =>
  assigned != null && (string.IsNullOrEmpty(native) || native == "None" ? assigned.Count == 0 || assigned.Count == 1 && assigned[0] == "None" : assigned.Count == 1 && assigned[0] == native);
 private static void MigrateProfiles() {
  if (!ready || PlayerInput.Profiles == null || PlayerInput.Profiles.Count == 0) return;
  string folder = Path.Combine(Main.SavePath, "DingDong"), marker = Path.Combine(folder, "controls-v1.json");
  try {
   HashSet<string> done = File.Exists(marker) ? JsonSerializer.Deserialize<HashSet<string>>(File.ReadAllText(marker)) : new();
   // The first native input profile can predate mod key registration. Complete
   // its mod entries without resetting any vanilla key or existing assignment.
   foreach (var profile in PlayerInput.Profiles.Values) foreach (ModKeybind key in AllKeys())
    profile.InputModes[InputMode.Keyboard].KeyStatus.TryAdd(KeyFullName(key), new List<string>());
   if (PlayerInput.CurrentProfile != null) foreach (ModKeybind key in AllKeys())
    PlayerInput.CurrentProfile.InputModes[InputMode.Keyboard].KeyStatus.TryAdd(KeyFullName(key), new List<string>());
   var pending = PlayerInput.Profiles.Where(p => !done.Contains(p.Key)).ToArray();
   if (pending.Length == 0) return;
   Directory.CreateDirectory(folder);
   string original = Path.Combine(Main.SavePath, "input profiles.json");
   if (File.Exists(original)) File.Copy(original, Path.Combine(folder, "controls-before-v1-" + Guid.NewGuid().ToString("N") + ".json"));
   int changed = 0;
   foreach (var profile in pending) {
    var config = profile.Value.InputModes[InputMode.Keyboard];
    var status = config.KeyStatus;
    var unloaded = (Dictionary<string, List<string>>)typeof(KeyConfiguration).GetField("UnloadedModKeyStatus", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(config);
    // Native tML saves no empty key lists. A profile without any mod assignment
    // is the only fresh profile we can identify without overwriting custom unbindings.
    bool Meaningful(KeyValuePair<string, List<string>> pair) => pair.Key.Contains('/') && pair.Value.Any(k => k != "None") &&
     !(pair.Key == "InfiniteMunitions/RotateAmmo" && pair.Value.SequenceEqual(new[] { "Q" }));
    var registered = AllKeys().Select(KeyFullName).ToHashSet();
    bool fresh = !status.Any(Meaningful) && !unloaded.Any(p => registered.Contains(p.Key) && Meaningful(p));
    // InfiniteMunitions registers during Load and stores a second copy in the
    // native unloaded-key table. Native Save would otherwise overwrite the new
    // binding with that stale copy, then restore Q on the next Load.
    foreach (ModKeybind key in AllKeys()) if (unloaded.Remove(KeyFullName(key), out var saved)) {
     var keys = status[KeyFullName(key)]; if (keys.Count == 0) keys.AddRange(saved);
    }
    changed += ApplyBindings(status, fresh);
    done.Add(profile.Key);
   }
   if (changed > 0) PlayerInput.Save();
   string temp = marker + ".pending"; File.WriteAllText(temp, JsonSerializer.Serialize(done)); File.Move(temp, marker, true);
  } catch (Exception ex) { DingDongBiomeHomes.Instance.Logger.Warn("DINGDONG_CONTROLS migration deferred: " + ex.GetType().Name); }
 }
 private static int ApplyBindings(Dictionary<string, List<string>> status, bool fresh) {
  int changed = 0;
  if (fresh) foreach (ModKeybind key in AllKeys()) {
   string binding = KeyDefault(key);
   if (string.IsNullOrEmpty(binding) || binding == "None" || !status.TryGetValue(KeyFullName(key), out var keys) || keys.Count != 0) continue;
   keys.Add(binding); changed++;
  }
  foreach (var change in changes.Values) {
   if (!status.TryGetValue(KeyFullName(change.Key), out var keys) || !IsNativeDefault(keys, change.Old)) continue;
   keys.Clear(); if (!string.IsNullOrEmpty(change.New)) keys.Add(change.New); changed++;
  }
  foreach (var pair in vanilla) {
   if (!status.TryGetValue(pair.Key, out var keys) || !IsNativeDefault(keys, pair.Value.Old)) continue;
   keys.Clear(); if (!string.IsNullOrEmpty(pair.Value.New)) keys.Add(pair.Value.New); changed++;
  }
  return changed;
 }
 internal static int RuntimeChecks() {
  if (!ready || changes.Count < 35) throw new InvalidOperationException("Pack controls were not applied.");
  if (!IsNativeDefault(new List<string> { "F" }, "F") || IsNativeDefault(new List<string> { "Mouse5" }, "F") || IsNativeDefault(new List<string>(), "F"))
   throw new InvalidOperationException("Custom key preservation failed.");
  foreach (var change in changes.Values) {
   if (KeyDefault(change.Key) != change.New) throw new InvalidOperationException("Default key was not updated.");
   List<string> assigned = change.Key.GetAssignedKeys(InputMode.Keyboard);
   if (string.IsNullOrEmpty(change.New) ? assigned.Count != 0 : assigned.Count != 1 || assigned[0] != change.New)
    throw new InvalidOperationException("Fresh native profile does not use the pack default: " + KeyFullName(change.Key) + "=" + string.Join(",", assigned));
  }
  var custom = changes.Keys.ToDictionary(k => k, _ => new List<string> { "Mouse5" });
  custom["CalamityMod/AdrenalineMode"].Clear(); ApplyBindings(custom, false);
  if (custom["CalamityMod/AdrenalineMode"].Count != 0 || custom.Any(p => p.Key != "CalamityMod/AdrenalineMode" && !p.Value.SequenceEqual(new[] { "Mouse5" })))
   throw new InvalidOperationException("Existing custom assignments or unbindings changed.");
  foreach (var pair in vanilla) {
   var assigned = PlayerInput.CurrentProfile.InputModes[InputMode.Keyboard].KeyStatus[pair.Key];
   if (string.IsNullOrEmpty(pair.Value.New) ? assigned.Count != 0 : !assigned.SequenceEqual(new[] { pair.Value.New }))
    throw new InvalidOperationException("Vanilla key conflict remains: " + pair.Key);
  }
  return changes.Count + vanilla.Count + 4;
 }
 public override void Unload() { loadHook?.Dispose(); loadHook = null; changes.Clear(); ready = false; }
}
