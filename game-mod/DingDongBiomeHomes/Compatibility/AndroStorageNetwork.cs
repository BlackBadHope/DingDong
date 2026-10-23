using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using DingDongBiomeHomes.Worldgen;
namespace DingDongBiomeHomes.Compatibility;

internal static class AndroStorageNetwork
{
 internal const byte Request = 78, Snapshot = 79;
 private static Action<int, int> update;
 private static Action<byte, int, int> probeTransport;
 internal static void Install(List<ILHook> hooks) {
  Type mod = R19CompatibilitySystem.Provider("androLib", "1.0.7.17", "androLib.AndroMod");
  if (mod == null) return;
  if (mod.Module.ModuleVersionId != Guid.Parse("bccfb088-dd08-484a-a0ad-a6454167adc4")) throw new InvalidOperationException("androLib network identity changed.");
  Type vacuum = mod.Assembly.GetType("androLib.Common.Globals.VacuumToStorageItem", true);
  update = R19CompatibilitySystem.Method(vacuum, "UpdateItemSpaceStatus").CreateDelegate<Action<int, int>>();
  hooks.Add(new ILHook(R19CompatibilitySystem.Method(vacuum, "SendItemSpaceStatusToServer"), il => {
   il.Body.Instructions.Clear(); il.Body.ExceptionHandlers.Clear(); il.Body.Variables.Clear();
   var c = new ILCursor(il); c.Emit(OpCodes.Ldarg_0); c.Emit(OpCodes.Ldarg_1); c.EmitDelegate<Action<int, int>>(Send); c.Emit(OpCodes.Ret);
  }));
  hooks.Add(new ILHook(R19CompatibilitySystem.Method(mod, "HandlePacket"), il => {
   Instruction original = il.Body.Instructions[0]; var c = new ILCursor(il);
   c.Emit(OpCodes.Ldarg_1); c.Emit(OpCodes.Ldarg_2); c.EmitDelegate<Func<BinaryReader, int, bool>>(LegacyRequest);
   c.Emit(OpCodes.Brfalse, original); c.Emit(OpCodes.Ret);
  }));
 }
 internal static bool ValidItem(int encoded) {
  int index = encoded >= 0 ? encoded : -encoded - 1;
  return (uint)index < Main.maxItems;
 }
 private static void Send(int encoded, int ignore) {
  if (!ValidItem(encoded) || (uint)Main.myPlayer >= Main.maxPlayers) return;
  update(Main.myPlayer, encoded);
  if (Main.netMode != NetmodeID.MultiplayerClient) return;
  if (probeTransport != null) { probeTransport(Request, Main.myPlayer, encoded); return; }
  ModPacket packet = DingDongBiomeHomes.Instance.GetPacket(); packet.Write(Request); packet.Write((short)encoded); packet.Send(-1, ignore);
 }
 internal static void Handle(byte kind, BinaryReader source, int sender) {
  try { HandleBody(kind, source, sender); }
  catch (IOException) { ContinentalSectionPacketEnvelope.Discard(source); Reject(sender, kind, "Frame"); }
 }
 private static void HandleBody(byte kind, BinaryReader source, int sender) {
  using BinaryReader reader = ContinentalSectionPacketEnvelope.Read(source);
  int expected = kind == Request ? 2 : 3;
  if (reader.BaseStream.Length != expected) { Reject(sender, kind, "Length"); return; }
  if (kind == Request) {
   if (Main.netMode != NetmodeID.Server || (uint)sender >= Main.maxPlayers || Main.player[sender] == null || !Main.player[sender].active) { Reject(sender, kind, "Sender"); return; }
   int encoded = reader.ReadInt16(); if (!ValidItem(encoded)) { Reject(sender, kind, "Item"); return; }
   update(sender, encoded); Relay(sender, encoded);
  } else {
   if (Main.netMode != NetmodeID.MultiplayerClient) { Reject(sender, kind, "Direction"); return; }
   int player = reader.ReadByte(), encoded = reader.ReadInt16();
   if ((uint)player >= Main.maxPlayers || !ValidItem(encoded)) { Reject(sender, kind, "ItemOrPlayer"); return; }
   update(player, encoded);
  }
 }
 private static bool LegacyRequest(BinaryReader source, int sender) {
  if (Main.netMode != NetmodeID.Server || !source.BaseStream.CanSeek) return false;
  long position = source.BaseStream.Position;
  if (source.ReadByte() != 0) { source.BaseStream.Position = position; return false; }
  Handle(Request, source, sender); return true;
 }
 private static void Relay(int player, int encoded) {
  if (probeTransport != null) { probeTransport(Snapshot, player, encoded); return; }
  ModPacket packet = DingDongBiomeHomes.Instance.GetPacket(); packet.Write(Snapshot); packet.Write((byte)player); packet.Write((short)encoded); packet.Send(-1, player);
 }
 private static void Reject(int sender, byte kind, string reason) => PacketRejectGuard.Reject("ANDRO_STORAGE", sender, kind, reason);
 internal static int RuntimeChecks(Player player) {
  if (Environment.GetEnvironmentVariable("DINGDONG_FULL_CLIENT_PROBE") != "1") throw new InvalidOperationException("Disposable network probe required.");
  Type type = ModLoader.GetMod("androLib").Code.GetType("androLib.Common.Globals.VacuumToStorageItem", true);
  const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
  FieldInfo cacheField = type.GetField("playerItemSpaceStatus", flags), requestsField = type.GetField("MyRequests", flags);
  var oldCache = cacheField.GetValue(null); var oldRequests = requestsField.GetValue(null);
  int net = Main.netMode, id = Main.myPlayer; Player peer = Main.player[1]; bool active = player.active;
  int sent = 0, sentPlayer = -1, sentItem = -1, checks = 0;
  void Require(bool ok, string name) { if (!ok) throw new InvalidOperationException(name); checks++; }
  void Receive(byte kind, int sender, params byte[] data) { using var reader = new BinaryReader(new MemoryStream(data)); Handle(kind, reader, sender); }
  var cache = new (uint, Dictionary<int, uint>)[Main.maxItems + 1];
  for (int i = 0; i < cache.Length; i++) cache[i] = (0, new Dictionary<int, uint>());
  try {
   cacheField.SetValue(null, cache); requestsField.SetValue(null, new SortedSet<int>());
   Main.myPlayer = 0; player.active = true; Main.player[1] = new Player { active = true, whoAmI = 1 };
   probeTransport = (_, p, item) => { sent++; sentPlayer = p; sentItem = item; };
   Main.netMode = NetmodeID.MultiplayerClient; Send(5, -1);
   Require(sent == 1 && sentPlayer == 0 && sentItem == 5 && cache[5].Item2.ContainsKey(0), "Native andro item-space send keeps its real player identity");
   Main.netMode = NetmodeID.Server; Receive(Request, 1, 6, 0);
   Require(sent == 2 && sentPlayer == 1 && sentItem == 6 && cache[6].Item2.ContainsKey(1), "Server accepts and relays the native storage request");
   Receive(Request, 1, 6, 0, 9); Receive(Request, Main.maxPlayers, 6, 0); Receive(Request, 1, 255, 127); Receive(Snapshot, 1, 1, 7, 0);
   Require(sent == 2 && !cache[7].Item2.ContainsKey(1), "Invalid length, sender, item index and direction cannot update or relay storage state");
   Main.netMode = NetmodeID.MultiplayerClient; Receive(Snapshot, Main.maxPlayers, 1, 8, 0);
   Require(cache[8].Item2.ContainsKey(1) && !cache[8].Item2.ContainsKey(Main.maxPlayers), "Client snapshot records the actual peer instead of the server sentinel");
   Receive(Snapshot, Main.maxPlayers, 1, 247, 255);
   Require(!cache[8].Item2.ContainsKey(1), "Negative native item-space encoding removes its peer entry");
   Main.netMode = NetmodeID.Server;
   using (var reader = new BinaryReader(new MemoryStream(new byte[] { 0, 9, 0 }))) ModLoader.GetMod("androLib").HandlePacket(reader, 1);
   Require(sent == 3 && cache[9].Item2.ContainsKey(1), "Original androLib ItemSpaceSync server entry no longer throws");
  } finally {
   probeTransport = null; cacheField.SetValue(null, oldCache); requestsField.SetValue(null, oldRequests);
   Main.netMode = net; Main.myPlayer = id; Main.player[1] = peer; player.active = active;
  }
  return checks;
 }
 internal static void Reset() => update = null;
}
